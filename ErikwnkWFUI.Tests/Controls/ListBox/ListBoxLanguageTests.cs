﻿using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// The list box and the titled one have no text of their own: the items and the title come from the caller, the
/// number in front of an item is a number. So there is nothing to translate in them. What a language switch can
/// still touch: the text that was handed over stays as it was, a longer German text (with umlauts) and an ampersand
/// are painted as written, and a list that is shown carries on through a switch. [Collection]: the tests switch the
/// language.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class ListBoxLanguageTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private T Show<T>(T control, int width = 220, int height = 150) where T : Control
    {
        control.Bounds = new Rectangle(5, 5, width, height);

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(width + 10, height + 10)
        };
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    [Fact]
    public void TheTextsHandedOver_StayAsTheyWere_WhenTheLanguageChanges()
    {
        string english = "";
        TitledListBox list = null!;

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            english = UIStrings.Get("MessageBox.Cancel");
            list = UIStyles.TitledListBoxes.CreateStandard(english);
            list.Items.Add(english);
        });

        using (list)
        {
            LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
            {
                Assert.Equal(english, list.Title);
                Assert.Equal(english, list.Items[0]);
                Assert.NotEqual(UIStrings.Get("MessageBox.Cancel"), list.Title);
            });
        }
    }

    [Fact]
    public void EveryLanguage_TakesTheTextsAsTheyAre()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                string text = "Eintrag für " + language + " & Söhne - Größe ÄÖÜ";

                using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard();
                listBox.Items.Add(text);
                Assert.Equal(text, listBox.InvokePrivate<string>("GetDisplayText", listBox.Items[0]));

                using TitledListBox titled = UIStyles.TitledListBoxes.CreateStandard(text);
                Assert.Equal(text, titled.Title);
            });
        }
    }

    [Fact]
    public void TheListBoxes_HaveNoKeyOfTheirOwn_InTheTranslations()
    {
        // The library's own texts live in the two tables of UIStrings, keyed by control name; none belongs to a list box.
        foreach (string table in new[] { "English", "German" })
        {
            System.Reflection.FieldInfo field = typeof(UIStrings).GetField(table, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            Dictionary<string, string> keys = (Dictionary<string, string>)field.GetValue(null)!;

            Assert.NotEmpty(keys);
            Assert.DoesNotContain(keys.Keys, key => key.StartsWith("ListBox.") || key.StartsWith("TitledListBox."));
        }
    }

    [Fact]
    public void AnAmpersandInAnItem_AndInTheTitle_IsPaintedAsWritten()
    {
        // The item is drawn as written (no mnemonic), the title label does not take an ampersand for one either.
        using TitledListBox list = Show(UIStyles.TitledListBoxes.CreateStandard("Q&A"));
        list.Items.Add("Fish & Chips");

        Assert.Equal("Q&A", list.Title);
        Assert.False(list.GetPrivateField<Label>("_titleLabel")!.UseMnemonic);
    }

    [Fact]
    public void ALongGermanItem_WithUmlauts_IsPaintedInTheSameWayAsAnEnglishOne()
    {
        string german = "Die Geschwindigkeitsbegrenzung auf Bundesautobahnen ist ein häufig diskutiertes Thema der Verkehrspolitik";
        string english = "The speed limit on federal motorways is a frequently discussed subject in the politics of transport";

        foreach (string text in new[] { german, english })
        {
            using WfuiListBox listBox = Show(UIStyles.ListBoxes.CreateStandard(allowReorder: true, showEnumeration: true));
            listBox.Items.Add(text);

            using Bitmap bitmap = new Bitmap(listBox.Width, listBox.Height);
            listBox.DrawToBitmap(bitmap, new Rectangle(0, 0, listBox.Width, listBox.Height));

            // Something is painted in the row, away from its start (the text goes on into the second line).
            bool ink = false;

            for (int x = 40; x < 140 && !ink; x++)
            {
                for (int y = 2; y < listBox.ItemHeight - 2 && !ink; y++)
                    ink = bitmap.GetPixel(x, y).GetBrightness() > 0.6f;
            }

            Assert.True(ink, "no text painted for " + text.Substring(0, 10));
        }
    }

    [Fact]
    public void AShownList_CarriesOnThroughALanguageSwitch()
    {
        using TitledListBox list = Show(UIStyles.TitledListBoxes.CreateStandard("Songs", allowReorder: true, showEnumeration: true));
        list.Items.Add("Erster");
        list.Items.Add("Zweiter");
        list.SelectedIndex = 1;

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                Application.DoEvents();

                Assert.Equal(1, list.SelectedIndex);
                Assert.Equal(2, list.Items.Count);
                Assert.Equal("Songs", list.Title);
            });
        }
    }
}
