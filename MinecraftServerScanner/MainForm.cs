using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace WindowsFormsApp1
{
  


    public partial class MainForm : Form
    {
        MinecraftServerScanner serverScanner = new MinecraftServerScanner();

        public MainForm()
        {
            InitializeComponent();





            textBox1.Text= "54.39.158.149,51.178.176.74,51.91.62.101,146.59.252.149";
            numericUpDown1.Value = 40000;
            numericUpDown2.Value = 42000;
            numericUpDown3.Value = 300;
            //NetworkScanner ns = new NetworkScanner("51.178.176.74","51.178.176.100", 40799);

            // Create two groups for the ListView
            // ListViewGroup group1 = new ListViewGroup("Group 1", HorizontalAlignment.Left);
            //ListViewGroup group2 = new ListViewGroup("Group 2", HorizontalAlignment.Left);

            // Add the groups to the ListView
            //listView1.Groups.Add(group1);
            //listView1.Groups.Add(group2);

            // Set the ListView to show groups
            //listView1.ShowGroups = true;
            // Attach the MouseDoubleClick event handler
            listView1.MouseDoubleClick += ListView1_MouseDoubleClick;
            // Handle the ColumnClick event to enable sorting.
            listView1.ColumnClick += ListView1_ColumnClick;
            // Create an instance of the custom ListViewItemSorter and assign it to the ListView.

            //   listView1.ListViewItemSorter = new ListViewItemComparer(columnToSort, orderOfSort);

            SetTransparencyForControls(this.Controls);
            this.Opacity=1;





        }

        // Function to set transparency for controls recursively
        private void SetTransparencyForControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Label label)
                {
                    // Set the background color of the label to be transparent
                    label.BackColor = System.Drawing.Color.Transparent;
                }
               
            }
        }


        private SortOrder orderOfSort;
        private int columnToSort;
        private void ListView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {

            // Determine whether the column is already the column that is being sorted.
            if (e.Column == columnToSort)
            {
                // Reverse the current sort direction for this column.
                orderOfSort = (orderOfSort == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted and default to ascending.
                columnToSort = e.Column;
                orderOfSort = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            listView1.Sort();
        }


        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Check if an item is selected
            if (listView1.SelectedItems.Count > 0)
            {
                // Get the selected item
                ListViewItem selectedItem = listView1.SelectedItems[0];

                // Get the text of the selected item
                string selectedText = selectedItem.Text+":"+ selectedItem.SubItems[1].Text;

                // Copy the selected text to the clipboard
                Clipboard.SetText(selectedText);

                // Optionally, show a message to indicate that the text has been copied
                MessageBox.Show("Text copied to clipboard: " + selectedText, "Copy Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            if (serverScanner.running) {
                return;
            }
            listView1.Items.Clear();

            string[] ipAddresses = textBox1.Text.Split(','); // Replace with your desired IP addresses
            int startPort = (int)numericUpDown1.Value;
            int endPort = (int)numericUpDown2.Value; // Adjust the port range as needed

           
            Dictionary<string, string> serverVersions = await serverScanner.ScanServersAsync(ipAddresses, startPort, endPort,progressBar1,listView1,(int)numericUpDown3.Value);

        }

// Function to export ListView items to CSV
        private void ExportListViewToCsv(ListView listView, string filePath)
        {
            try
            {
                // Create or overwrite the CSV file
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write the column headers to the CSV
                    writer.WriteLine(string.Join(";", listView.Columns.Cast<ColumnHeader>().Select(column => column.Text)));

                    // Iterate through each ListViewItem and write its sub-items to the CSV
                    foreach (ListViewItem item in listView.Items)
                    {
                        var values = item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(subItem => subItem.Text);
                        writer.WriteLine(string.Join(";", values));
                    }
                }

                MessageBox.Show("ListView data has been exported to CSV successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportListViewToCsv(listView1, saveFileDialog.FileName);
            }
        }
    }

    public class TransparentListView : ListView
    {
        private int alpha = 80; // Adjust the alpha value to control transparency

        public TransparentListView()
        {
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Bitmap bmp = new Bitmap(this.Width, this.Height))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Draw the ListView background with reduced opacity
                ControlPaint.DrawImageDisabled(g, this.BackgroundImage, 0, 0, Color.Transparent);

                // Draw the ListView items and sub-items
                base.OnPaint(new PaintEventArgs(g, e.ClipRectangle));

                // Apply the alpha value to the entire control
                Color transparentColor = Color.FromArgb(alpha, this.BackColor);
                e.Graphics.DrawImage(bmp, 0, 0);
                e.Graphics.FillRectangle(new SolidBrush(transparentColor), this.ClientRectangle);
            }
        }
    }
    public class ListViewItemComparer : System.Collections.IComparer
    {
        private int column;
        private SortOrder sortOrder;

        public ListViewItemComparer()
        {
            column = 0; // Default to sorting the first column
            sortOrder = SortOrder.Ascending; // Default to ascending order
        }

        public ListViewItemComparer(int column, SortOrder order)
        {
            this.column = column;
            this.sortOrder = order;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;

            // You should implement error checking here if the columns don't exist or have incorrect data

            // Get the sub-item text to compare
            string textX = itemX.SubItems[column].Text;
            string textY = itemY.SubItems[column].Text;

            // Perform a comparison based on the sort order
            int compareResult = string.Compare(textX, textY);

            // Invert the result for descending order
            if (sortOrder == SortOrder.Descending)
            {
                compareResult = -compareResult;
            }

            return compareResult;
        }
    }
}


/* 
        string[] ipAddresses = { "51.178.176.74" }; // Replace with your desired IP addresses
        int startPort = 40500;
        int endPort = 41000; // Adjust the port range as needed

        MinecraftServerScanner serverScanner = new MinecraftServerScanner();
        Dictionary<string, string> serverVersions = await serverScanner.ScanServersAsync(ipAddresses, startPort, endPort);

       /* foreach (var kvp in serverVersions)
        {
            Console.WriteLine($"Server {kvp.Key}: {kvp.Value}");
        }*/