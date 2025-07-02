using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Browser_Reviewer
{
    public partial class Form_LabelManager : Form
    {
        public Form_LabelManager()
        {
            InitializeComponent();
            LoadLabelsFromDataBase();
            dataGridView1.CellClick += dataGridView1_CellClick;
            dataGridView1.CellFormatting += dataGridView1_CellFormatting;



        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Validar índice
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Verificar que la columna sea Label_color
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Label_color")
            {
                // Abrir selector de color
                using (ColorDialog colorDialog = new ColorDialog())
                {
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        Color selectedColor = colorDialog.Color;

                        // Aplicar color a la celda
                        var cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                        cell.Style.BackColor = selectedColor;
                        cell.Style.ForeColor = selectedColor; // ocultar texto

                        // Guardar como int en el DataTable
                        dataGridView1.Rows[e.RowIndex].Cells["Label_color"].Value = selectedColor.ToArgb();
                    }
                }
            }
        }



        private void LoadLabelsFromDataBase()
        {
            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                // Cargar los datos al DataTable
                Helpers.dataAdapter = new SQLiteDataAdapter("SELECT Label_name, Label_color FROM Labels", connection);
                //Esta linea sobra pq en al funcion de save creamos el builder
                //SQLiteCommandBuilder builder = new SQLiteCommandBuilder(Helpers.dataAdapter);

                Helpers.labelsTable = new DataTable();
                Helpers.dataAdapter.Fill(Helpers.labelsTable);

                // Asignar el DataTable como DataSource
                dataGridView1.DataSource = Helpers.labelsTable;

                // Cambiar los encabezados si quieres
                dataGridView1.Columns["Label_name"].HeaderText = "Name";
                dataGridView1.Columns["Label_color"].HeaderText = "Color";

                // Desactivar la fila en blanco al final
                dataGridView1.AllowUserToAddRows = false;

                // Aplicar color a cada celda según su valor
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var cell = row.Cells["Label_color"];
                    if (cell.Value != null && int.TryParse(cell.Value.ToString(), out int colorValue))
                    {
                        Color color = Color.FromArgb(colorValue);
                        cell.Style.BackColor = color;
                        cell.Style.ForeColor = color; // ocultar texto haciendo texto del mismo color
                        //cell.Value = ""; // eliminar número mostrado
                    }
                }
            }
            dataGridView1.ClearSelection();
        }


        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {

            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count)
                return;
            // Verificamos que estamos en la columna "Label_color"
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Label_color")
            {
                if (e.Value != null && int.TryParse(e.Value.ToString(), out int colorValue))
                {
                    Color color = Color.FromArgb(colorValue);

                    // Aplicar fondo y texto invisible
                    dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = color;
                    dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = color;

                    // Ocultar el valor formateado (lo que se muestra)
                    e.Value = ""; // Esto borra visualmente el número
                    e.FormattingApplied = true;
                }
            }
        }




        private void LabelManager_Load(object sender, EventArgs e)
        {
        }




        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button_addLabel_Click(object sender, EventArgs e)
        {

            if (Helpers.labelsTable == null)
            {
                MessageBox.Show("The table is not loaded.");
                return;
            }

            // Crear nueva fila
            DataRow nuevaFila = Helpers.labelsTable.NewRow();
            nuevaFila["Label_name"] = "new label";
            nuevaFila["Label_color"] = Color.Black.ToArgb(); // guardamos el color como int

            // Agregarla al DataTable
            Helpers.labelsTable.Rows.Add(nuevaFila);

            // Aplicar estilos visuales en el DataGridView
            int nuevaFilaIndex = dataGridView1.Rows.Count - 1;

            var celdaTexto = dataGridView1.Rows[nuevaFilaIndex].Cells["Label_name"];
            celdaTexto.Style.Font = new Font(dataGridView1.Font, FontStyle.Bold);

            var celdaColor = dataGridView1.Rows[nuevaFilaIndex].Cells["Label_color"];
            celdaColor.Style.BackColor = Color.Black;
            celdaColor.Style.ForeColor = Color.Black; // mismo color para ocultar texto
            //celdaColor.Value = ""; // ocultar el número


        }

        private void button_saveLabel_Click(object sender, EventArgs e)
        {


            if (Helpers.labelsTable == null)
            {
                MessageBox.Show("There is no data to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SQLiteConnection connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                try
                {
                    connection.Open();

                    // Crear un nuevo DataAdapter para la conexión activa
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter("SELECT Label_name, Label_color FROM Labels", connection))
                    {
                        SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);
                        adapter.Update(Helpers.labelsTable);
                    }

                    MessageBox.Show("Changes saved successfully.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while saving changes:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }


        }

        private void button_deleteLabel_Click(object sender, EventArgs e)
        {

            // Verificar que hay una fila seleccionada
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Select a label to delete.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Obtener el nombre de la etiqueta
            var label = dataGridView1.CurrentRow.Cells["Label_name"].Value?.ToString();
            if (string.IsNullOrEmpty(label))
            {
                MessageBox.Show("Failed to retrieve the label name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Confirmar con el usuario
            var confirm = MessageBox.Show($"Are you sure you want to delete the label \"{label}\"?\n\nThis action will update references in other tables.",
                              "Confirm Deletion",
                              MessageBoxButtons.YesNo,
                              MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            // Llamar al método centralizado
            DeleteLabelsCleanReferences(label); // o "Ninguno" si prefieres otro valor
        }


        private void DeleteLabelsCleanReferences(string labelToDelete)
        {
            using (var connection = new SQLiteConnection(Helpers.chromeViewerConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var relatedTables = new List<(string table, string field)>
                {
                    ("results", "Label"),
                    ("firefox_results", "Label"),
                    ("firefox_downloads", "Label"),
                    ("chrome_downloads", "Label"),
                    ("bookmarks_Firefox", "Label"),
                    ("bookmarks_Chrome", "Label"),
                    ("autofill_data", "Label"),
                };

                        // 1. Reemplazar el label con NULL en todas las tablas relacionadas
                        foreach (var (table, field) in relatedTables)
                        {
                            string updateQuery = $"UPDATE {table} SET {field} = @replacement WHERE {field} = @label";
                            using (var cmd = new SQLiteCommand(updateQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@replacement", DBNull.Value);
                                cmd.Parameters.AddWithValue("@label", labelToDelete);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 2. Eliminar el label de la tabla principal
                        string deleteQuery = "DELETE FROM Labels WHERE Label_name = @label";
                        using (var deleteCmd = new SQLiteCommand(deleteQuery, connection, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("@label", labelToDelete);
                            deleteCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show("Label deleted and references updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Refrescar etiquetas
                        LoadLabelsFromDataBase();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Error al eliminar la etiqueta:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void button_ok_Click(object sender, EventArgs e)
        {
            Form openForm = Application.OpenForms.OfType<Form_LabelManager>().FirstOrDefault();
            if (openForm != null)
                openForm.Close();
        }
    }
}


