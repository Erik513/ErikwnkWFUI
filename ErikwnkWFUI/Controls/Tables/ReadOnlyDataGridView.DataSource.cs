using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // Changes to the data source made by the grid itself: putting the items in a new order, batched changes, the state after binding.
    public partial class ReadOnlyDataGridView
    {

        protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)
        {
            base.OnDataBindingComplete(e);

            // Binding to a new DataSource replaces the row collection
            // entirely, which resets per-row visual state - reapplying
            // here keeps alternating-row/selection colors correct for
            // whatever just got bound in, not just whatever existed at
            // construction time.
            ApplyStyles();

            // Every row-order/count-changing operation - a fresh bind, a
            // sort (CycleSort/ReorderDataSource below), or one of
            // DataGridView's own paste/insert/delete operations in a
            // subclass - ends up here via ResetBindings(), so renumbering
            // unconditionally (not just for a fresh bind, like the
            // bookkeeping below the early-return guard) is what keeps this
            // column's numbers live-correct after every one of them.
            RenumberEnumeration();

            if (_isApplyingInternalDataChange)
            {
                // This Reset was caused by ApplyBatchedDataSourceChange
                // rewriting the bound list's contents (CycleSort/
                // ReorderDataSource here for a sort, or one of
                // DataGridView's own editing operations in a subclass),
                // not a real rebind - the bookkeeping below would
                // otherwise stomp on whatever state that change is
                // applying right now.
                return;
            }

            // A Reset is a fresh bind (DataSource assigned, or the bound
            // list reset) - that's the only time a "no cell selected on
            // load" default and a fresh original-order snapshot make sense.
            // Incremental changes (a row added/edited) leave both alone, so
            // typing into the grid doesn't fight the user's own selection.
            if (e.ListChangedType == ListChangedType.Reset)
            {
                ClearSelection();
                CurrentCell = null;

                // Binding normally happens before the control is ever shown,
                // and WinForms auto-selects the first cell the first time a
                // grid with rows but no CurrentCell receives focus - which
                // happens later, as part of the containing form being shown.
                // Clearing it again once actually visible is what defeats
                // that (setting it here alone doesn't stick).
                _clearSelectionOnNextVisible = true;

                _originalOrder = new List<object>();
                foreach (DataGridViewRow row in Rows)
                {
                    if (row.DataBoundItem != null)
                    {
                        _originalOrder.Add(row.DataBoundItem);
                    }
                }

                _sortedColumnIndex = -1;
                _sortOrder = SortOrder.None;

                foreach (DataGridViewColumn column in Columns)
                {
                    column.HeaderCell.SortGlyphDirection = SortOrder.None;
                }

                UpdateEnumerationHeaderText();
            }
        }


        // The "type here to add a row" placeholder committing to a real row
        // (AllowUserToAddRows, only ever true on DataGridView) goes through
        // WinForms' own IBindingList.AddNew()/commit flow directly, never
        // through ApplyBatchedDataSourceChange/ResetBindings() - so
        // OnDataBindingComplete's own renumber above never runs for it,
        // and the enumeration column was left showing stale numbers (and
        // no number at all for the newly-committed row) until some
        // unrelated sort/insert/delete happened to trigger a renumber.
        protected override void OnUserAddedRow(DataGridViewRowEventArgs e)
        {
            base.OnUserAddedRow(e);
            RenumberEnumeration();
        }



        // DataGridView.Sort(IComparer) is a no-op for the display order when
        // the control is data bound (it only works unbound, or against a
        // source that implements IBindingListView) - so instead, the bound
        // list itself is rewritten in the desired order. Any IList data
        // source works, not just BindingList&lt;T&gt;.
        private void ReorderDataSource(List<object> desiredOrder)
        {
            if (!(DataSource is IList list))
            {
                return;
            }

            ApplyBatchedDataSourceChange(list, () =>
            {
                list.Clear();

                foreach (object item in desiredOrder)
                {
                    list.Add(item);
                }
            });
        }


        private readonly struct DataSourceReflectionInfo
        {
            public DataSourceReflectionInfo(PropertyInfo raiseListChangedEventsProperty, MethodInfo resetBindingsMethod)
            {
                RaiseListChangedEventsProperty = raiseListChangedEventsProperty;
                ResetBindingsMethod = resetBindingsMethod;
            }

            public PropertyInfo RaiseListChangedEventsProperty { get; }
            public MethodInfo ResetBindingsMethod { get; }
        }


        private static DataSourceReflectionInfo GetDataSourceReflectionInfo(Type dataSourceType)
        {
            return DataSourceReflectionCache.GetOrAdd(dataSourceType, type => new DataSourceReflectionInfo(
                type.GetProperty("RaiseListChangedEvents", AnyInstanceMember),
                type.GetMethod("ResetBindings", AnyInstanceMember, null, Type.EmptyTypes, null)));
        }


        // Inserts newItems at insertAtIndex via Clear() + re-Add() in the
        // exact final order - a direct list.Insert(index, item) at a
        // hand-picked index was tried first and found to corrupt the
        // grid's internal row/cell bookkeeping even though the resulting
        // list CONTENTS were correct (a later, unrelated click threw
        // InvalidOperationException out of WinForms' own code). Used by
        // DataGridView's own row-growing editing operations (paste,
        // insert row above/below) for the same reason. Callers are
        // expected to already be running inside an
        // ApplyBatchedDataSourceChange batch - this only rewrites the
        // list, it doesn't suppress/replay change notifications itself.
        //
        // originalItems is a snapshot the CALLER takes, not one this
        // method derives from list itself - list already contains
        // newItems by the time every caller here gets to call this
        // (AddNew() appends to the bound list immediately, before this
        // runs). Snapshotting list at that point would already include
        // them at the tail, and the trailing copy loop below would then
        // re-add that same tail - duplicating every one of newItems once.
        protected static void InsertItemsAt(
            IList list, List<object> originalItems, int insertAtIndex, IEnumerable<object> newItems)
        {
            list.Clear();

            for (int i = 0; i < insertAtIndex; i++)
            {
                list.Add(originalItems[i]);
            }

            foreach (object newItem in newItems)
            {
                list.Add(newItem);
            }

            for (int i = insertAtIndex; i < originalItems.Count; i++)
            {
                list.Add(originalItems[i]);
            }
        }


        // The snapshot InsertItemsAt's own originalItems parameter expects
        // - must be taken BEFORE any AddNew() call the caller makes, per
        // InsertItemsAt's own remarks above on why.
        protected static List<object> SnapshotItems(IList list)
        {
            List<object> items = new List<object>(list.Count);

            foreach (object item in list)
            {
                items.Add(item);
            }

            return items;
        }


        // Runs a multi-step change against the bound list (list.Clear()+Add()
        // for a reorder, or whatever a subclass's own editing operations do)
        // with change notifications suppressed, then raises exactly one
        // Reset via ResetBindings() at the end (both BindingList&lt;T&gt;
        // members - public and protected respectively, reached via
        // reflection the same way EnableDoubleBuffering reaches a
        // protected Control property). Without this, each individual
        // Clear()/Add()/Remove() call gets picked up and repainted
        // immediately - including whatever intermediate state
        // DataGridView reacts to along the way (e.g. the list being
        // briefly empty mid-reorder) - which is what previously showed up
        // as the first row flashing "selected". Protected (not private)
        // so a subclass's own editing operations can share this exact
        // same batching, not just this class's own sorting.
        protected void ApplyBatchedDataSourceChange(IList list, Action mutate)
        {
            OnApplyingBatchedDataSourceChange();

            // ResetBindingsMethod below is exactly what DataGridView treats
            // as a fresh rebind - the same Reset notification a brand new
            // DataSource assignment would raise - which snaps the scroll
            // position back to the top-left the same way a real rebind
            // does, even though this change (a sort, a paste, a row
            // insert/delete) has nothing to do with actually rebinding.
            // Capturing both scroll properties before the change and
            // restoring them after (clamped to whatever's still valid once
            // the row count has potentially changed) is what keeps this
            // from silently scrolling the user back to the top of a long
            // grid on every one of those operations.
            int firstDisplayedScrollingRowIndex = Rows.Count > 0 ? FirstDisplayedScrollingRowIndex : -1;
            int horizontalScrollingOffset = HorizontalScrollingOffset;

            object dataSource = DataSource;
            DataSourceReflectionInfo reflectionInfo = GetDataSourceReflectionInfo(dataSource.GetType());
            PropertyInfo raiseEventsProperty = reflectionInfo.RaiseListChangedEventsProperty;
            bool canSuppressEvents =
                raiseEventsProperty != null && raiseEventsProperty.CanRead && raiseEventsProperty.CanWrite;
            bool previousRaiseEvents = true;

            if (canSuppressEvents)
            {
                previousRaiseEvents = (bool)raiseEventsProperty.GetValue(dataSource);
                raiseEventsProperty.SetValue(dataSource, false);
            }

            _isApplyingInternalDataChange = true;

            try
            {
                mutate();
            }
            finally
            {
                // Stays true through ResetBindings() below, not just the
                // mutation above - ResetBindings() is what actually raises
                // the (suppressed-until-now) Reset notification, so it
                // re-enters OnDataBindingComplete on this same call stack.
                // Flipping the flag off before that would let that
                // reentrant call run the "fresh bind" bookkeeping again
                // mid-change - wiping out the glyph/sort-order CycleSort
                // just set, and calling ClearSelection() / CurrentCell =
                // null while the grid is mid-reset, which is exactly the
                // kind of reentrant state change DataGridView can throw
                // InvalidOperationException over.
                if (canSuppressEvents)
                {
                    raiseEventsProperty.SetValue(dataSource, previousRaiseEvents);
                    reflectionInfo.ResetBindingsMethod?.Invoke(dataSource, null);
                }

                _isApplyingInternalDataChange = false;
            }

            // FirstDisplayedScrollingRowIndex throws for an out-of-range
            // value rather than clamping itself (unlike
            // HorizontalScrollingOffset, which clamps silently) - guarded
            // for both an empty grid (nothing to scroll to) and a row
            // count that shrank (e.g. a delete) past where it used to be.
            if (firstDisplayedScrollingRowIndex >= 0 && Rows.Count > 0)
            {
                FirstDisplayedScrollingRowIndex = Math.Min(firstDisplayedScrollingRowIndex, Rows.Count - 1);
            }

            HorizontalScrollingOffset = horizontalScrollingOffset;

            ClearSelection();
            CurrentCell = null;
        }


        // Hook for a subclass to drop any of its own per-row visual state
        // (DataGridView overrides this to forget its hovered delete-row
        // index) right before a batched change runs - rows can shift
        // position without the mouse moving (e.g. the hovered row itself
        // gets deleted, or a sort reorders things underneath the cursor),
        // and no mouse-move means no fresh OnCellMouseEnter/Leave to
        // correct a now-stale index. This class itself has no such state
        // of its own, so the base implementation is empty.
        protected virtual void OnApplyingBatchedDataSourceChange()
        {
        }
    }
}
