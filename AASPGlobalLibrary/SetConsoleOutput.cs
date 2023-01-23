using System.Text;

//Use this to override Console.Write to a textbox so that you can see log output in WinForms.
//Make sure to define IsRichTextBox to false first if you are not using a rich text box
namespace AASPGlobalLibrary
{
    public class SetConsoleOutput
    {
        public class ControlWriter : TextWriter
        {
            internal static bool IsRichTextBox;
            readonly Control textbox;
            internal ControlWriter(Control textbox)
            {
                this.textbox = textbox;
            }

            public override void Write(char value)
            {
                try { textbox.Text += value; }
                catch { textbox.Invoke(() => value); }
                if (IsRichTextBox) (textbox as RichTextBox).SelectionStart = textbox.Text.Length;
                else (textbox as TextBox).SelectionStart = textbox.Text.Length;
            }
#pragma warning disable CS8765
            public override void Write(string value)
            {
                try { textbox.Text += value; }
                catch { textbox.Invoke(() => value); }
                if (IsRichTextBox) (textbox as RichTextBox).SelectionStart = textbox.Text.Length;
                else (textbox as TextBox).SelectionStart = textbox.Text.Length;
            }
#pragma warning restore CS8765
            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }

        public SetConsoleOutput(RichTextBox richTextBox)
        {
            ControlWriter.IsRichTextBox = true;
            Console.SetOut(new ControlWriter(richTextBox));
        }
        public SetConsoleOutput(TextBox richTextBox)
        {
            ControlWriter.IsRichTextBox = false;
            Console.SetOut(new ControlWriter(richTextBox));
        }
    }
}