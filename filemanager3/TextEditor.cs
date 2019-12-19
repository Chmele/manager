using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace filemanager3
{
    public partial class Form2 : Form
    {
        TextProcessor proc;
        bool textSaved;
        public Form2(string path)
        {
            InitializeComponent();
            proc = new TextProcessor(path);
            ShowFileText();
            textSaved = true;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            
        }
        public void ShowFileText()
        {
            textBox1.Text = proc.text;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            proc.Save(textBox1.Text);
            textSaved = true;
        }

        private void ShowFileList(object sender, EventArgs e)
        {
            proc.Save(textBox1.Text);
            textSaved = true;
            listView1.Items.Clear();
            foreach (string s in proc.AllFiles())
                listView1.Items.Add(s);
        }

        private void ShowTagList(object sender, EventArgs e)
        {
            proc.Save(textBox1.Text);
            textSaved = true;
            listView1.Items.Clear();
            foreach (HtmlTag t in proc.AllTags())
                listView1.Items.Add(proc.text.Substring(t.startIndex - 1, t.endIndex + 1));
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            string s = "Складання списку файлів, гіпертекстові посилання на які містяться у файлі формату HTML"
                + "\nЗлиття текстових та інших файлів"
                + "\nСкладання списку тегів у файлі формату HTML та заміна кількох обраних на інші."
                + "\nЗа заданими словами (курсором) виділяла із заздалегідь заданого списку найбільш схоже слово за першими кількома літерами. Передбачити повідомлення " +
                "'неоднозначно', якщо є кілька схожих слів, та вивести їх. Список слів є у вхідному файлі test_lst.txt ";
            MessageBox.Show(s);       
        }

        private void ListView1_Click(object sender, EventArgs e)
        {
            proc.Save(textBox1.Text);
            int clicked = listView1.SelectedItems[0].Index;
            HtmlTag tag = proc.tags[clicked];
            textBox1.Text = proc.text.Remove(tag.startIndex, tag.endIndex).Insert(tag.startIndex,textBox2.Text + ">");
            ShowTagList(sender, e);
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!textSaved)
                if (MessageBox.Show("Зберегти файл?", "Підтвердити", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    proc.Save(textBox1.Text);
                }
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            textSaved = false;
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                    case Keys.F4:
                        List<string> list = proc.Similar(textBox1.SelectedText);
                        string output = "";
                        foreach (string s in list)
                            output += s + "\n";
                        MessageBox.Show(output);
                        break;
            }
        }

        private void Form2_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
    public class TextProcessor
    {
        public string text;
        public readonly string path;
        public List<HtmlTag> tags = new List<HtmlTag>();
        public TextProcessor(string path)
        {
            this.path = path;
            StreamReader file = System.IO.File.OpenText(path);
            string s = "";
            while (s != null)
            {
                s = file.ReadLine();
                text += s;
                text += "\r\n";
            }
            file.Close();
        }
        public void Save(string to_save)
        {
            text = to_save;
            var file = new StreamWriter(path);
            file.WriteLine(to_save);
            file.Close();
        }
        public List<string> AllFiles()
        {
            var list = new List<string>();
            int index = text.IndexOf("<img", 0);
            while (index != -1 && index < text.Length)
            {
                int index1 = text.IndexOf('"', text.IndexOf("src", index)) + 1;
                int index2 = text.IndexOf('"', index1) - index1;
                list.Add(text.Substring(index1, index2));
                index = text.IndexOf("<img", index + 1);
            }
            return list;
        }
        public List<HtmlTag> AllTags()
        {
            tags.Clear();
            int index = text.IndexOf("<", 0);
            while (index != -1 && index < text.Length)
            {
                int index1 = index + 1;
                int index2 = text.IndexOf(">", index1) - index1;
                tags.Add(new HtmlTag(index1, index2 + 1));
                index = text.IndexOf("<", index + 1);
            }
            return tags;
        }
        public List<string> Similar(string check)
        {
            
            var list = new List<string>();
            var result = new List<string>();
            var similarity = new List<int>();
            StreamReader file = System.IO.File.OpenText("test_lst.txt");
            string s = "";
            int max = 0;
            while (s != null)
            {
                s = file.ReadLine();
                if (s != null && s != "")
                {
                    list.Add(s);
                    similarity.Add(StringCompare(check, s));
                    int count = similarity.Count - 1;
                    if (similarity[count] > max) max = similarity[count];
                }
            }
            file.Close();
            for (int i = 0; i < list.Count; i++) 
                if (similarity[i] == max) result.Add(list[i]);
            return result;
        }
        public static int StringCompare(string s1, string s2)
        {
            int i = 0;
            if (s1 == "") return 0;
            while (i < s1.Length && i < s2.Length && s1[i] == s2[i]) i++;
            return i;
        }
    }
    public class HtmlTag
    {
        public int startIndex;
        public int endIndex;
        public HtmlTag(int start, int end)
        {
            startIndex = start;
            endIndex = end;
        }
    }
}
