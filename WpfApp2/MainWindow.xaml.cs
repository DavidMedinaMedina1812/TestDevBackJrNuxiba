using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Data.SqlTypes;
using System.Security.Cryptography.X509Certificates;
using CsvHelper;
using System.Globalization;
using System.IO;

namespace WpfApp2
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Mantiene la conexión a la base de datos
        SqlConnection sqlConnection;

        public MainWindow()
        {

            InitializeComponent();
            EstablecerConexiónConBD();

            //Se inicializa la aplicación con los 10 datos top mostrados en el listbox
            MuestraTop10();
        }


        //Función que muestra los el top 10 usuarios en pantalla (primeros 10) 
        private void MuestraTop10()
        {
            try
            {
                string query = "SELECT TOP 10 u.*, e.Sueldo, e.FechaIngreso FROM usuarios u INNER JOIN empleados e ON u.userId = e.userId";
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, sqlConnection);

                using (sqlDataAdapter)
                {
                    DataTable storeTable = new DataTable();
                    sqlDataAdapter.Fill(storeTable);

                    // Assuming topList is a ListBox
                    topList.DisplayMemberPath = ""; // Clear any previous setting

                    // Define a DataTemplate to show multiple columns
                    DataTemplate dataTemplate = new DataTemplate();

                    // Create a StackPanel to hold multiple TextBlocks (one for each column)
                    FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                    stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

                    //Añade TextBlocks para cada columna con un margen
                    foreach (DataColumn column in storeTable.Columns)
                    {
                        FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding(column.ColumnName));

                        //Añade un margen
                        textBlockFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(5, 0, 5, 0));

                        stackPanelFactory.AppendChild(textBlockFactory);
                    }

                    //Ajusta el StackPanel 
                    dataTemplate.VisualTree = stackPanelFactory;

                    //Ajusta el DataTemplate con el ItemTemplate del ListBox
                    topList.ItemTemplate = dataTemplate;

                    //Ajusta el data source
                    topList.ItemsSource = storeTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        //Función que crea un CSV con todos los datos de la base de datos
        private void GenerarCSV(object sender, RoutedEventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["WpfApp2.Properties.Settings.pruebaBackConnectionString"].ConnectionString;

            // Inicializa la conexión
            sqlConnection = new SqlConnection(connectionString);

            string query = "SELECT * FROM usuarios u INNER JOIN empleados e ON u.userId = e.userId";
            //Se ingresa manualmente el path para evitar el tener datos hardcodeados en el código
            string outputPath = PathText.Text;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(command);
                        DataTable dataTable = new DataTable();
                        sqlDataAdapter.Fill(dataTable);

                        //Escribe el dataTable al CSV
                        using (TextWriter writer = new StreamWriter(outputPath))
                        {
                            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                            {
                                //Escribe el encabezado
                                foreach (DataColumn column in dataTable.Columns)
                                {
                                    csv.WriteField(column.ColumnName);
                                }
                                csv.NextRecord();

                                //Escribe los datos
                                foreach (DataRow row in dataTable.Rows)
                                {
                                    foreach (DataColumn column in dataTable.Columns)
                                    {
                                        csv.WriteField(row[column]);
                                    }
                                    csv.NextRecord();
                                }
                            }
                        }
                    }
                }

                MessageBox.Show("CSV generado exitosamente!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    

        //Función ue agrega a un usuario
        private void AgregarUsuario(object sender, RoutedEventArgs e)
        {
            try
            {
                //Se crea lista de parámetros
                List<SqlParameter> parameters = new List<SqlParameter>(){
                    new SqlParameter("@Login", SqlDbType.VarChar) {Value = LoginTB.Text},
                    new SqlParameter("@Nombre", SqlDbType.VarChar) {Value = NombreTB.Text},
                    new SqlParameter("@Paterno", SqlDbType.VarChar) {Value = ApellidPatTB.Text},
                    new SqlParameter("@Materno", SqlDbType.VarChar) {Value = ApellidoMatTB.Text},
                    new SqlParameter("@Sueldo", SqlDbType.Float)
                    {
                        Value = float.TryParse(SueldoTB.Text, out float sueldoValue) ? (object)sueldoValue : DBNull.Value
                    },
                    new SqlParameter("@FechaIngreso", SqlDbType.Date) {Value = DateTime.Now.Date}

                };

                string query = "INSERT INTO Usuarios VALUES (@Login, @Nombre, @Paterno, @Materno)"
                                + "INSERT INTO Empleados VALUES (@Sueldo, @FechaIngreso)";
                                

                SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);
                sqlConnection.Open();

                sqlCommand.Parameters.AddRange(parameters.ToArray());

                DataTable storeTable = new DataTable();

                using (SqlDataAdapter adapter = new SqlDataAdapter(sqlCommand)) adapter.Fill(storeTable);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
                MessageBox.Show("Usuario agregado exitosamente!");
            }
        }


        //Función que actualiza el salario de un usuario en base a su id
        private void ActualizaSueldo(object sender, RoutedEventArgs e)
        {
            try
            {
                //Se obtienen el id y el sueldo de los textboxes
                int userId = int.TryParse(IdText.Text, out int userIdValue) ? userIdValue : -1; 
                float newSueldo = float.TryParse(SalarioCambiarText.Text, out float sueldoValue) ? sueldoValue : -1; 

                if (userId == -1 || newSueldo == -1)
                {
                    MessageBox.Show("Invalid input. Please enter valid UserId and Sueldo values.");
                    return;
                }

                //Se realiza un Update de sueldo en la base de datos
                string updateQuery = "UPDATE Empleados SET Sueldo = @NewSueldo WHERE userId = @UserId";
                SqlCommand updateCommand = new SqlCommand(updateQuery, sqlConnection);

                updateCommand.Parameters.AddWithValue("@NewSueldo", newSueldo);
                updateCommand.Parameters.AddWithValue("@UserId", userId);

                sqlConnection.Open();
                int rowsAffected = updateCommand.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    MessageBox.Show("Sueldo actualizado exitosamente!");
                    //Si se modifica algun usuario del top, automáticamente se modificará su sueldo en la tabla
                    MuestraTop10();
                }
                else
                {
                    MessageBox.Show("UserId no encontrado o sueldo no actualizafdo.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        //Función para conectarse con la base de datos
        private void EstablecerConexiónConBD()
        {
            string connectionString =
            ConfigurationManager.ConnectionStrings[
            "WpfApp2.Properties.Settings.pruebaBackConnectionString"
            ].ConnectionString;

            //Inicializa la conexión
            sqlConnection = new SqlConnection(connectionString);
        }

        private void PathText_TextChanged(object sender, object e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

    }
}
