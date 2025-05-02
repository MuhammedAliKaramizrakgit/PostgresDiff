using System;
using System.Drawing;
using System.Windows.Forms;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Runtime.InteropServices;

public class DiffViewer : UserControl
{
    private TableLayoutPanel layout;
    private RichTextBox oldTextBox;
    private RichTextBox newTextBox;
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

    public DiffViewer()
    {
        layout = new TableLayoutPanel()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        oldTextBox = new RichTextBox()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Consolas", 10),
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both
        };

        newTextBox = new RichTextBox()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Consolas", 10),
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both
        };

        layout.Controls.Add(oldTextBox, 0, 0);
        layout.Controls.Add(newTextBox, 1, 0);

        oldTextBox.SelectionChanged += HighlightCursorLine;
        newTextBox.SelectionChanged += HighlightCursorLine;

        oldTextBox.VScroll += (s, e) => SyncScroll(oldTextBox, newTextBox);
        newTextBox.VScroll += (s, e) => SyncScroll(newTextBox, oldTextBox);


        Controls.Add(layout);
    }
    public nint? LastHandle = null;
    private void SyncScroll(RichTextBox source, RichTextBox target)
    {
        if (isSyncingScroll) return;
        if (source.Handle  == LastHandle)
        {
            LastHandle = null;
            return;

        }
        isSyncingScroll = true;

        int scrollPos = GetScrollPos(source.Handle, SB_VERT);
        int targetScrollPos = GetScrollPos(target.Handle, SB_VERT);

        if (scrollPos != targetScrollPos)
        {
            SetScrollPos(target.Handle, SB_VERT, scrollPos, true);
            PostMessage(target.Handle, WM_VSCROLL, (IntPtr)(SB_THUMBPOSITION + 0x10000 * scrollPos), IntPtr.Zero);
            LastHandle = target.Handle;
        }

        isSyncingScroll = false;
    }
    bool HighlightCursorLineauto = false; 
    private void HighlightCursorLine(object sender, EventArgs e)
    {
        if (HighlightCursorLineauto == true || isSyncingScroll == true) return;

         RichTextBox box = sender as RichTextBox;
        int line = box.GetLineFromCharIndex(box.SelectionStart);
        HighlightCursorLineauto = true;
        if (line != lastHighlightedLine)
        {
            ClearHighlights(oldTextBox);
            ClearHighlights(newTextBox);

            HighlightLine(oldTextBox, line);
            HighlightLine(newTextBox, line);

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

    public void CompareTexts(string oldText, string newText)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(oldText, newText);

        oldTextBox.Clear();
        newTextBox.Clear();

        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    newTextBox.SelectionColor = Color.Green;
                    newTextBox.AppendText("+ " + line.Text + "\n");
                     oldTextBox.AppendText("+ " + new string(' ', line.Text.Length) + "\n");
                    break;
                case ChangeType.Deleted:
                    oldTextBox.SelectionColor = Color.Red;
                    oldTextBox.AppendText("- " + line.Text + "\n");
                    newTextBox.AppendText("+ " + new string(' ', line.Text.Length) + "\n");
                    break;
                case ChangeType.Unchanged:
                    oldTextBox.SelectionColor = Color.Black;
                    newTextBox.SelectionColor = Color.Black;
                    oldTextBox.AppendText("  " + line.Text + "\n");
                    newTextBox.AppendText("  " + line.Text + "\n");
                    break;
            }
        }
    }
}
