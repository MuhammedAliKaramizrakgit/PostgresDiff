namespace PostgresDiff
{
    public partial class CompareForm : Form
    {
        
        public CompareForm()
        {
            InitializeComponent();
           

          

           
        }
        public void init2(DatabaseObject compatexts) 
        {
            var diffViewer = new MultiDiffViewer(compatexts.ListOneDataBase. Count());
            diffViewer.Dock = DockStyle.Fill;
            this.Controls.Add(diffViewer);
            
            
                diffViewer.CompareTexts(compatexts);
            

        }

    }
}
