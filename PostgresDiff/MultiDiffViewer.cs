using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
namespace PostgresDiff
{
    public class MultiDiffViewer : UserControl
    {
        private TableLayoutPanel layout;
        private List<RichTextBox> textBoxes = new List<RichTextBox>();
        private List<TextBox> headerTextBoxes = new List<TextBox>();
        private bool isSyncingScroll = false;
        private int lastHighlightedLine = -1;

        [DllImport("user32.dll")]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int SB_VERT = 1;
        private const int WM_VSCROLL = 0x115;
        private const int SB_THUMBPOSITION = 4;

        public MultiDiffViewer(int textCount)
        {
            layout = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = textCount,
                RowCount = 2, // artık 2 satır var
                AutoSize = true
            };

            // Satır yüksekliklerini ayarla (ilk satır otomatik, ikinci satır fill)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // header TextBox için
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // RichTextBox için

            for (int i = 0; i < textCount; i++)
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / textCount));

                // Header TextBox (tek satır)
                var headerBox = new TextBox()
                {
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI", 9),
                    Margin = new Padding(2),
                    Name = $"headerTextBox_{i}"
                };
                headerTextBoxes.Add(headerBox);
                layout.Controls.Add(headerBox, i, 0); // 0. satır

                // RichTextBox (içerik)
                var textBox = new RichTextBox()
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Font = new Font("Consolas", 10),
                    WordWrap = false,
                    ScrollBars = RichTextBoxScrollBars.Both
                };

                textBox.SelectionChanged += HighlightCursorLine;
                textBox.VScroll += (s, e) => SyncScroll(textBox);

                textBoxes.Add(textBox);
                layout.Controls.Add(textBox, i, 1); // 1. satır
            }

            Controls.Add(layout);
        }

        public nint? LastHandle = null;
        private void SyncScroll(RichTextBox source)
        {
            if (isSyncingScroll) return;

            if (source.Handle == LastHandle)
            {
                LastHandle = null;
                return;

            }
            isSyncingScroll = true;

            int scrollPos = GetScrollPos(source.Handle, SB_VERT);
            foreach (var textBox in textBoxes)
            {
                if (textBox != source)
                {
                    SetScrollPos(textBox.Handle, SB_VERT, scrollPos, true);
                    PostMessage(textBox.Handle, WM_VSCROLL, (IntPtr)(SB_THUMBPOSITION + 0x10000 * scrollPos), IntPtr.Zero);
                    LastHandle = textBox.Handle;
                }
            }

            isSyncingScroll = false;
        }
        bool HighlightCursorLineauto = false;
        private void HighlightCursorLine(object sender, EventArgs e)
        {
            if (HighlightCursorLineauto == true || isSyncingScroll == true) return;
            HighlightCursorLineauto = true;
            RichTextBox box = sender as RichTextBox;
            int line = box.GetLineFromCharIndex(box.SelectionStart);

            if (line != lastHighlightedLine)
            {
                foreach (var textBox in textBoxes)
                {
                    ClearHighlights(textBox);
                    HighlightLine(textBox, line);
                }
                lastHighlightedLine = line;
            }
            HighlightCursorLineauto = false;
        }

        private void HighlightLine(RichTextBox box, int line)
        {
            int start = box.GetFirstCharIndexFromLine(line);
            if (start < 0) return;

            int length = (line < box.Lines.Length - 1)
                ? box.GetFirstCharIndexFromLine(line + 1) - start
                : box.Text.Length - start;

            box.Select(start, length);
            box.SelectionBackColor = Color.LightGray;
            box.DeselectAll();
        }

        private void ClearHighlights(RichTextBox box)
        {

            int selectionStart = box.SelectionStart;
            int selectionLength = box.SelectionLength;

            box.SelectAll();
            box.SelectionBackColor = Color.White;
            box.Select(selectionStart, selectionLength);
        }


        bool loading = true;
        public void CompareTextsRawLineByLine(params DatabaseObject[] dbObjects)
        {
            if (dbObjects.Length != textBoxes.Count)
                throw new ArgumentException("Obje sayısı, oluşturulmuş RichTextBox sayısıyla eşleşmiyor.");

            var splitLines = dbObjects.Select(db =>
            {
                var oneDb = db.ListOneDataBase[0]; // Base olarak her zaman ilkini kullan
                return oneDb.SqlText.Replace("\r", "").Split('\n').ToList();
            }).ToList();

            int maxLineCount = splitLines.Max(list => list.Count);

            for (int i = 0; i < dbObjects.Length; i++)
            {
                var oneDb = dbObjects[i].ListOneDataBase[0];
                headerTextBoxes[i].Text = oneDb.connectionItem.Name;
                textBoxes[i].Clear();
            }

            for (int lineIndex = 0; lineIndex < maxLineCount; lineIndex++)
            {
                var lineGroup = new List<string>();
                for (int i = 0; i < splitLines.Count; i++)
                {
                    lineGroup.Add(lineIndex < splitLines[i].Count ? splitLines[i][lineIndex] : "");
                }

                bool allSame = lineGroup.Distinct().Count() == 1;

                for (int i = 0; i < textBoxes.Count; i++)
                {
                    var tb = textBoxes[i];
                    tb.SelectionColor = allSame ? Color.Black : Color.Red;
                    tb.AppendText((allSame ? "  " : "> ") + lineGroup[i] + "\n");
                }
            }
        }


        public void CompareTexts(DatabaseObject dbObject)
        {
            var oneDatabases = dbObject.ListOneDataBase;

            if (oneDatabases.Count != textBoxes.Count)
                throw new ArgumentException("Verilen bağlantı sayısı, RichTextBox sayısıyla eşleşmiyor.");

            var diffBuilder = new InlineDiffBuilder(new Differ());
            var baseText = oneDatabases[0].SqlText;

            var allLineLists = new List<List<string>>();

            foreach (var db in oneDatabases)
            {
                var currentText = db.SqlText;
                var diff = diffBuilder.BuildDiffModel(baseText, currentText);

                var lines = new List<string>();
                foreach (var line in diff.Lines)
                {
                    lines.Add(line.Type switch
                    {
                        ChangeType.Unchanged => "  " + line.Text,
                        ChangeType.Inserted => "+ " + line.Text,
                        ChangeType.Deleted => "- " + line.Text,
                        _ => "  "
                    });
                }
                allLineLists.Add(lines);
            }

            int maxLineCount = allLineLists.Max(list => list.Count);
            foreach (var list in allLineLists)
                while (list.Count < maxLineCount) list.Add("");

            for (int i = 0; i < oneDatabases.Count; i++)
            {
                headerTextBoxes[i].Text = oneDatabases[i].connectionItem.Name;
                textBoxes[i].Clear();
            }

            for (int lineIndex = 0; lineIndex < maxLineCount; lineIndex++)
            {
                for (int j = 0; j < textBoxes.Count; j++)
                {
                    var line = allLineLists[j][lineIndex];
                    var tb = textBoxes[j];

                    tb.SelectionColor = line.StartsWith("+") ? Color.Green :
                                        line.StartsWith("-") ? Color.Red : Color.Black;

                    tb.AppendText(line + "\n");
                }
            }
        }



        public void CompareTwoTexts(DatabaseObject left, DatabaseObject right)
        {
            if (textBoxes.Count != 2)
                throw new InvalidOperationException("Bu yöntem sadece 2 RichTextBox ile çalışır.");

            var leftDb = left.ListOneDataBase[left.SelectedDatabase];
            var rightDb = right.ListOneDataBase[right.SelectedDatabase];

            var diffBuilder = new SideBySideDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(leftDb.SqlText, rightDb.SqlText);

            var leftBox = textBoxes[0];
            var rightBox = textBoxes[1];

            headerTextBoxes[0].Text = leftDb.connectionItem.Name;
            headerTextBoxes[1].Text = rightDb.connectionItem.Name;

            leftBox.Clear();
            rightBox.Clear();

            int lineCount = Math.Max(diff.OldText.Lines.Count, diff.NewText.Lines.Count);
            for (int i = 0; i < lineCount; i++)
            {
                var leftLine = i < diff.OldText.Lines.Count ? diff.OldText.Lines[i] : new DiffPiece(string.Empty, ChangeType.Imaginary);
                var rightLine = i < diff.NewText.Lines.Count ? diff.NewText.Lines[i] : new DiffPiece(string.Empty, ChangeType.Imaginary);

                leftBox.SelectionColor = GetColorForChangeType(leftLine.Type);
                leftBox.AppendText(Prefix(leftLine.Type) + leftLine.Text + "\n");

                rightBox.SelectionColor = GetColorForChangeType(rightLine.Type);
                rightBox.AppendText(Prefix(rightLine.Type) + rightLine.Text + "\n");
            }

            Color GetColorForChangeType(ChangeType type) => type switch
            {
                ChangeType.Inserted => Color.Green,
                ChangeType.Deleted => Color.Red,
                _ => Color.Black,
            };

            string Prefix(ChangeType type) => type switch
            {
                ChangeType.Inserted => "+ ",
                ChangeType.Deleted => "- ",
                _ => "  ",
            };
        }




    }
}





