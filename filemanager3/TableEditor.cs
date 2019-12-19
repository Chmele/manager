using System;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace filemanager3
{
    public partial class Form3 : Form
    {
        Table table;
        int columns = 20;
        int rows = 20;
        string path;
        bool formulaSaved;
        public Form3(string path)
        {
            InitializeComponent();
            formulaSaved = true;
            WindowState = FormWindowState.Maximized;
            this.path = path;
            table = new Table(path, rows, columns);
            FillDataGrid();
        }
        private void FillDataGrid()
        {
            dataGridView1.RowHeadersWidth = 60;
            dataGridView1.ColumnCount = columns;
            dataGridView1.RowCount = rows;
            for (int i = 1; i <= columns;)
                dataGridView1.Columns[i - 1].Name = "C" + i++.ToString();
            for (int i = 1; i <= rows;)
                dataGridView1.Rows[i - 1].HeaderCell.Value = "R" + i++.ToString();
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    if (!table.Cells[i, j].isNull) 
                        dataGridView1.Rows[i].Cells[j].Value = table.Cells[i,j].value;
                    else dataGridView1.Rows[i].Cells[j].Value = "";
        }
        private void DataGridView1_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Focused)
                if (!formulaSaved)
                {
                    DialogResult res = MessageBox.Show("Зберегти зміни у формулі?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (res == DialogResult.Yes)
                        if (!table.error)
                            SaveFromTextBox();
                        else MessageBox.Show("У формулі помилка");
                }

        }
        private void DataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView1_CellLeave(sender, e);
            textBox1.Text = table.Cells[e.RowIndex, e.ColumnIndex].text;
            formulaSaved = true;
        }

        private void DataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            formulaSaved = false;
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
       {
            if (e.KeyCode == Keys.Enter)
            {
                SaveFromTextBox();
            }
        }
        private void SaveFromTextBox()
        {
            int X = dataGridView1.SelectedCells[0].ColumnIndex;
            int Y = dataGridView1.SelectedCells[0].RowIndex;
            table.Cells[Y, X].text = textBox1.Text;
            if (textBox1.Text != "")
                table.Cells[Y, X].isNull = false;
            else
                table.Cells[Y, X].isNull = true;
            table.EvaluateCells();
            if (table.error)
            {
                MessageBox.Show(table.message);
                formulaSaved = false;
            }
            else
            {
                FillDataGrid();
                formulaSaved = true;
            }
        }

        private void ShowVariant(object sender, EventArgs e)
        {
            MessageBox.Show(@"Варіант 2: 
Операції:
+, -, *, / (бінарні операції);
mod, dіv;
+, - (унарні операції);
іnc, dec;");
        }

        private void AddRow(object sender, EventArgs e)
        {
            dataGridView1.Rows.Add(new DataGridViewRow());
            rows++;
            table = new Table(path, rows, columns);
            FillDataGrid();
        }

        private void AddColumn(object sender, EventArgs e)
        {
            dataGridView1.Columns.Add(new DataGridViewColumn(dataGridView1.Rows[0].Cells[0]));
            columns++;
            table = new Table(path, rows, columns);
            FillDataGrid();
        }

        private void DeleteRow(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("Видалити рядок?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if(res == DialogResult.Yes)
            {
                if(rows == 1)
                {
                    MessageBox.Show("У таблиці один рядок", "Неможливо виконати операцію", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                rows--;
                dataGridView1.Rows.RemoveAt(rows-1);
                table.rows--;
                table.SaveTable(path);
                table = new Table(path, rows, columns);
                FillDataGrid();
            }
        }

        private void DeleteColumn(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("Видалити стовпчик?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                if (rows == 1)
                {
                    MessageBox.Show("У таблиці один стовпчик", "Неможливо виконати операцію", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                columns--;
                dataGridView1.Columns.RemoveAt(columns - 1);
                table.rows--;
                table.SaveTable(path);
                table = new Table(path, rows, columns);
                FillDataGrid();
            }
        }

        private void SaveTable(object sender, EventArgs e)
        {
            table.SaveTable(path);
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult res = MessageBox.Show("Зберегти зміни у таблиці?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                table.SaveTable(path);
            }
        }

        private void SaveAs(object sender, EventArgs e)
        {
            var path = new SaveFileDialog();
            path.ShowDialog();
            path.DefaultExt = ".xml";
            table.SaveTable(path.FileName);
        }
    }
    public class Table
    {
        public bool error;
        public string message;
        public Cell[,] Cells = { };
        private Parser parser;
        public int rows;
        public int columns;
        public Table (string path, int rows, int columns)
        {
            Cells = new Cell[rows, columns];
            this.rows = rows;
            this.columns = columns;
            ReadTable(path);
            parser = new Parser(Cells,rows, columns);
            EvaluateCells();
        }
        public void ReadTable(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    Cells[i, j] = new Cell();
            foreach (XmlElement table in xmlDoc.DocumentElement.ChildNodes)
            {
                foreach (XmlElement cellinfo in xmlDoc.DocumentElement.ChildNodes)
                {
                    Int32.TryParse(cellinfo.GetAttribute("X"), out int X);
                    Int32.TryParse(cellinfo.GetAttribute("Y"), out int Y);
                    Cells[X - 1, Y - 1] = new Cell(cellinfo.InnerText);
                }
            }
        }
        public void SaveTable(string path)
        {
           
            XDocument xdoc = new XDocument();
            XElement table = new XElement("table");
            XAttribute colAttr =new XAttribute("columns", columns.ToString());
            XAttribute rowAttr = new XAttribute("rows", rows.ToString());
            table.Add(colAttr);
            table.Add(rowAttr);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    if (!Cells[i, j].isNull)
                    {
                        XElement cell = new XElement("cell", Cells[i,j].text);
                        XAttribute XIndex = new XAttribute("X", (i + 1).ToString());
                        XAttribute YIndex = new XAttribute("Y", (j + 1).ToString());
                        cell.Add(XIndex);
                        cell.Add(YIndex);
                        table.Add(cell);
                    }
            xdoc.Add(table);
            xdoc.Save(path);
        }
        public void EvaluateCells()
        {
            error = false;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    if (Cells[i, j].text != "")
                    {
                        Cells[i, j].value = parser.Eval(Cells[i, j].text);
                        Parser.varsInFormula.Clear();
                        if (parser.Error)
                        {
                            error = true;
                            message = parser.message;
                            return;
                        }
                    }
        }
    }
    public class Cell
    {
        public string text;
        public double value;
        public bool isNull;
        public Cell(string text)
        {
            isNull = false;
            this.text = text;
            value = 0;
        }
        public Cell()
        {
            isNull = true;
            this.text = "";
            value = 0;
        }
    }
}
