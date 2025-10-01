using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Browser_Reviewer
{
    public partial class Form_Comments : Form
    {
        public Form_Comments()
        {
            InitializeComponent();
        }

        private void button_saveComment_Click(object sender, EventArgs e)
        {

            Helpers.comment = textBox_comments.Text;

            // Cerrar el formulario de comentarios si quieres
            this.Close();
            //Form openForm = Application.OpenForms.OfType<Form_Comments>().FirstOrDefault();
            //if (openForm != null)
            //    openForm.Close();
        }

        private void Form_Comments_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // o FixedDialog
            this.MaximizeBox = true;   // deja activo el botón Maximizar
            this.MinimizeBox = true;   // deja activo el botón Minimizar
            this.SizeGripStyle = SizeGripStyle.Hide; // oculta el grip de la esquina
        }
    }
}
