using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace filemanager3
{
    public partial class Form1 : Form
    {
        public static string currentPath = null;
        private PathView View = new PathView(currentPath);
        private CPD Manager = new CPD();
        public Form1()
        {
            InitializeComponent();
            ShowListView();
            FillDriveNodes();
            textBox1.Text = currentPath;
        }
        void ShowListView()
        {
            listView1.Clear();
            listView1.LargeImageList.Images.Add(Icon.ExtractAssociatedIcon("disk.ico"));
            listView1.LargeImageList.Images.Add(Icon.ExtractAssociatedIcon("folder.ico"));
            foreach (Element i in View.elements)
            {
                ListViewItem item = new ListViewItem();
                if (i is Disk)
                    listView1.Items.Add(i.Path, 0);
                if (i is Folder)
                    listView1.Items.Add(Path.GetFileName(i.Path), 1);
                if (i is File)
                {
                    listView1.LargeImageList.Images.Add(Icon.ExtractAssociatedIcon(i.Path));
                    listView1.Items.Add(Path.GetFileName(i.Path), listView1.LargeImageList.Images.Count - 1);
                }
            }
        }

        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int clicked = listView1.SelectedItems[0].Index;
            View.elements[clicked].Open();
            PathDraw();
        }

        private void Button1_MouseClick(object sender, MouseEventArgs e)
        {
            SetParentPath();
            PathDraw();
        }
        private void PathDraw()
        {
            View = new PathView(currentPath);
            textBox1.Text = currentPath;
            ShowListView();
        }

        public void SetParentPath() //надає currentPath значення шляху батька
        {
            if (currentPath == Path.GetPathRoot(currentPath))//батько - корінь файлової системи
                currentPath = null;
            else
            {
                currentPath = (currentPath + "\\").Substring(0, currentPath.LastIndexOf("\\"));//отримуємо шлях до батька
                if (currentPath.EndsWith(":")) currentPath += "\\";
            }
        }
        private void ListView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listView1.SelectedItems.Count != 0)
                {
                    int clicked = listView1.SelectedItems[0].Index;
                    listView1.FocusedItem = listView1.Items[clicked];
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        private void ElementContextOpen(object sender, EventArgs e)
        {
            int clicked = listView1.SelectedItems[0].Index;
            View.elements[clicked].Open();
            PathDraw();
        }

        private void ElementContextCopy(object sender, EventArgs e)
        {
            int clicked = listView1.SelectedItems[0].Index;
            Manager.SetOrigin(View.children[clicked]);
        }

        private void ElementContextPaste(object sender, EventArgs e)
        {
            Manager.Paste(currentPath);
            PathDraw();
        }

        private void ElementContextRename(object sender, EventArgs e)
        {
            listView1.SelectedItems[0].BeginEdit();
        }

        private void ListView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            int changed = listView1.SelectedItems[0].Index;
            Manager.Rename(View.children[changed], e.Label);
            PathDraw();
        }

        private void ListView1_KeyDown(object sender, KeyEventArgs e)
        {
            int clicked;
            try
            {
                switch (e.KeyCode)
                {
                    case Keys.Back:
                        SetParentPath();
                        PathDraw();
                        break;
                    case Keys.F3:
                        clicked = listView1.SelectedItems[0].Index;
                        View.elements[clicked].Open();
                        PathDraw();
                        break;
                    case Keys.F4:
                        Manager.CreateFolder(currentPath);
                        PathDraw();
                        break;
                    case Keys.F5:
                        Manager.Paste(currentPath);
                        PathDraw();
                        break;
                    case Keys.F6:
                        clicked = listView1.SelectedItems[0].Index;
                        Manager.SetOrigin(View.children[clicked]);
                        PathDraw();
                        break;
                    case Keys.F7:
                        clicked = listView1.SelectedItems[0].Index;
                        Manager.Delete(View.children[clicked]);
                        PathDraw();
                        break;
                    case Keys.F8:
                        Manager.CreateFile(currentPath);
                        PathDraw();
                        break;
                    case Keys.F9:
                        FileCombine();
                        PathDraw();
                        break;
                }
            }
            catch (Exception) { }
        }
        private void FileCombine()
        {
            var paths = new List<string>();
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                paths.Add(View.elements[item.Index].Path);
                //MessageBox.Show(View.elements[item.Index].Path);
            }
            Manager.FileCombine(paths);
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            string path = textBox1.Text;
            if (Directory.Exists(path))
                currentPath = path;
            PathDraw();
        }

        private void TreeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            e.Node.Nodes.Clear();
            string[] dirs;
            if (Directory.Exists(e.Node.FullPath))
            {
                dirs = Directory.GetDirectories(e.Node.FullPath);
                if (dirs.Length != 0)
                {
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        TreeNode dirNode = new TreeNode(new DirectoryInfo(dirs[i]).Name);
                        FillTreeNode(dirNode, dirs[i]);
                        e.Node.Nodes.Add(dirNode);
                    }
                }
            }
        }

        private void TreeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            e.Node.Nodes.Clear();
            string[] dirs;
            if (Directory.Exists(e.Node.FullPath))
            {
                dirs = Directory.GetDirectories(e.Node.FullPath);
                if (dirs.Length != 0)
                {
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        TreeNode dirNode = new TreeNode(new DirectoryInfo(dirs[i]).Name); FillTreeNode(dirNode, dirs[i]);
                        e.Node.Nodes.Add(dirNode);
                    }
                }
            }
        }
        private void FillDriveNodes()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                TreeNode driveNode = new TreeNode { Text = drive.Name };
                FillTreeNode(driveNode, drive.Name);
                treeView1.Nodes.Add(driveNode);
            }
        }
        // Отримуємо дочірні вузли для даного вузла
        private void FillTreeNode(TreeNode driveNode, string path)
        {
            try
            {
                TreeNode dirNode = new TreeNode();
                driveNode.Nodes.Add(dirNode);
            }
            catch (Exception) { }
        }
        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            currentPath = e.Node.FullPath;
            textBox1.Text = currentPath;
            PathDraw();
        }

        private void ElementContextDelete(object sender, EventArgs e)
        {
            int clicked = listView1.SelectedItems[0].Index;
            Manager.Delete(View.children[clicked]);
            PathDraw();
        }
    }
    public class CPD //CopyPasteDelete manager
    {
        private IChild ToCopy;
        public void SetOrigin(IChild item)
        {
            ToCopy = item;
        }
        public void Paste(string path)
        {
            ToCopy.Paste(path);
        }
        public void Delete(IChild item)
        {
            if (MessageBox.Show("Видалити?", "Підтвердити", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                item.Delete();
        }
        public void Rename(IChild item, string new_name)
        {
            item.Rename(new_name);
        }
        public void CreateFolder(string path)
        {
            if (path != null)
            {
                string new_path = Path.Combine(path, "Нова тека");
                int i = 1;
                string check = new_path;
                while (Directory.Exists(check))
                    check = new_path + "(" + i++.ToString() + ")";
                Directory.CreateDirectory(check);
            }
        }
        public void CreateFile(string path)
        {
            if (path != null)
            {
                string new_path = Path.Combine(path, "Новий файл");
                int i = 1;
                string check = new_path + ".txt";
                while (System.IO.File.Exists(check))
                    check = new_path + "(" + i++.ToString() + ")" + ".txt";
                var file = System.IO.File.Create(check);
                file.Close();
            }
        }
        internal void FileCombine(List<string> paths)
        {
            List<string> text = new List<string>();
            foreach (string s in paths)
            {
                var proc = new TextProcessor(s);
                //text.AddRange(proc.text);
                text.Add(proc.text);
            }
            var file = new StreamWriter(paths[0] + "merge" + ".txt", true, Encoding.UTF32);
            foreach (string s in text)
                file.WriteLine(s);
            file.Close();
        }
    }

    public class PathView
    {
        public List<Element> elements = new List<Element>();
        public List<IChild> children = new List<IChild>();
        public PathView(string path)
        {
            ClearPathView();
            if (path != null)
            {
                foreach (string item in Directory.GetDirectories(path))
                {
                    Folder f = new Folder(item);
                    children.Add(f);
                    elements.Add(f);
                }
                foreach (string item in Directory.GetFiles(path))
                {
                    
                    File f = new File(item);
                    children.Add(f);
                    elements.Add(f);
                }
            }
            else
                foreach (DriveInfo info in DriveInfo.GetDrives())
                    elements.Add(new Disk(info.Name));
        }
        public void ClearPathView()
        {
            elements.Clear();
            children.Clear();
        }
    }

    public abstract class Element
    {
        private string path;
        public string Path { get => path; set => path = value; }
        public abstract void Open();
    }
    public interface IChild
    {
        void Paste(string new_path);
        void Delete();
        void Rename(string new_name);

    }
    public class File : Element, IChild
    {
        public File(string item)
        {
            Path = item;
        }

        public void Paste(string new_path)
        {
            try
            {
                new_path = System.IO.Path.Combine(new_path, System.IO.Path.GetFileName(Path));
                if (new_path != Path) System.IO.File.Copy(Path, new_path, true);
            }
            catch (Exception) { }
        }

        public override void Open()
        {
            if (System.IO.Path.GetExtension(Path) == ".txt")
            {
                var t = new TxtFile(Path);
                t.Open();
            }
            else if (System.IO.Path.GetExtension(Path) == ".html")
            {
                var t = new HtmlFile(Path);
                t.Open();
            }
            else if (System.IO.Path.GetExtension(Path) == ".xml")
            {
                var t = new XmlFile(Path);
                t.Open();
            }
            else

            Process.Start(Path);
        }

        public void Delete()
        {
            System.IO.File.Delete(Path);
        }

        public void Rename(string new_name)
        {
            string father = (Path + "\\").Substring(0, Path.LastIndexOf("\\"));
            string new_path = System.IO.Path.Combine(father, new_name);
            string check = new_path;
            int i = 1;
            //string father = (Path + "\\").Substring(0, Path.LastIndexOf("\\"));//отримуємо шлях до батька
            if (!System.IO.File.Exists(new_path))
            {
                if (father.EndsWith(":")) father += "\\";
                if (!System.IO.File.Exists(System.IO.Path.Combine(father, new_name)))
                    System.IO.File.Move(Path, System.IO.Path.Combine(father, new_name));
            }
            else
            {
                while (System.IO.File.Exists(check))
                    check = new_path + "(" + i++.ToString() + ")" + ".txt";
                System.IO.File.Move(Path, System.IO.Path.Combine(father, check));
                //file.Close();
            }
        }
    }
    public class Folder : Element, IChild
    {
        public Folder(string path)
        {
            Path = path;
        }

        public void Delete()
        {
            try
            {
                foreach (string file in Directory.GetFiles(Path))
                    System.IO.File.Delete(file);
                foreach (string dir in Directory.GetDirectories(Path))
                    new Folder(dir).Delete();
                Directory.Delete(Path);
            }
            catch (Exception) { }
        }

        public override void Open()
        {
            Form1.currentPath = Path;
        }

        public void Rename(string new_name)
        {
            string new_path = Path;
            int i = 1;
            string check = new_path;
            while (Directory.Exists(System.IO.Path.Combine(Path, check)))
                check = new_name + "(" + i++.ToString() + ")";
            FileSystem.RenameDirectory(Path, check);

        }

        void IChild.Paste(string new_path)
        {
            try
            {
                //Створити всі директорії
                foreach (string dirPath in Directory.GetDirectories(Path, "*",
                    System.IO.SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(Path, new_path));
                //Копіювати всі файли та замінити з однаковим іменем
                foreach (string newPath in Directory.GetFiles(Path, "*.*",
                    System.IO.SearchOption.AllDirectories))
                    System.IO.File.Copy(newPath, newPath.Replace(Path, new_path), true);
            }
            catch (Exception) { }
        }
    }
    public class Disk : Element
    {
        public Disk(string name)
        {
            Path = name;
        }
        public override void Open()
        {
            Form1.currentPath = Path;
        }
    }
    public class TxtFile : File
    {
        public TxtFile(string item) : base(item)
        {
            Path = item;
        }
        public override void Open()
        {
            Form2 openfile = new Form2(Path);
            openfile.Show();
        }
    }
    public class HtmlFile : File
    {
        public HtmlFile(string item) : base(item)
        {
            Path = item;
        }
        public override void Open()
        {
            Form2 openfile = new Form2(Path);
            openfile.Show();
        }
    }
    public class XmlFile : File
    {
        public XmlFile(string item) : base(item)
        {
            Path = item;
        }
        public override void Open()
        {
            Form3 openfile = new Form3(Path);
            openfile.Show();
        }
    }
}
