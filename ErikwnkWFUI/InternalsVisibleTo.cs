using System.Runtime.CompilerServices;

// Grants the test project direct access to internal-but-not-control types
// (ColumnLayoutMath, ColumnFeatureSwitch, ...) shared between ListView and
// the DataGridView family - these aren't part of the library's own public
// API, just plumbing the tests need to reach without going through
// reflection (see PrivateReflection's own remarks on why that IS still used
// for the controls' own private members, where widening visibility would
// mean growing the real public/protected surface instead).
[assembly: InternalsVisibleTo("ErikwnkWFUI.Tests")]
