using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace master_floor
{
    public partial class Form2 : Form
    {
        private const string ConnectionString = "Host=localhost;Port=5432;Database=master_floor;Username=postgres;Password=root;";
        private NpgsqlConnection _connection;
        private NpgsqlDataAdapter _adapter;
        private DataTable _dataTable;
        private readonly Dictionary<string, Dictionary<int, string>> _lookupData = new Dictionary<string, Dictionary<int, string>>();

        public Form2()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            SetupComboBox();
        }

        private void InitializeDatabaseConnection()
        {
            _connection = new NpgsqlConnection(ConnectionString);
        }

        private void SetupComboBox()
        {
            comboBox1.Items.AddRange(new[]
            {
                "partner_products", "products", "partners",
                "material_types", "product_types", "materials", "users"
            });
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedTable = comboBox1.Text;
            if (string.IsNullOrEmpty(selectedTable)) return;

            try
            {
                LoadTableData(selectedTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadTableData(string tableName)
        {
            LoadLookupTables();

            _adapter = new NpgsqlDataAdapter($"SELECT * FROM {tableName}", _connection);
            var commandBuilder = new NpgsqlCommandBuilder(_adapter);

            _dataTable = new DataTable();
            _adapter.Fill(_dataTable);

            ConfigureDataGridView(tableName);
        }

        private void ConfigureDataGridView(string tableName)
        {
            dataGridView1.DataSource = null;
            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = _dataTable;

            HidePrimaryKeyColumn(tableName);
            ReplaceForeignKeyColumns(tableName);

            dataGridView1.DefaultValuesNeeded -= dataGridView1_DefaultValuesNeeded;
            dataGridView1.DefaultValuesNeeded += dataGridView1_DefaultValuesNeeded;
        }

        private void LoadLookupTables()
        {
            _lookupData["products"] = LoadLookupTable("products", "product_id", "product_name");
            _lookupData["partners"] = LoadLookupTable("partners", "partner_id", "partner_name");
            _lookupData["product_types"] = LoadLookupTable("product_types", "type_id", "product_type");
            _lookupData["material_types"] = LoadLookupTable("material_types", "type_id", "material_type");
        }

        private Dictionary<int, string> LoadLookupTable(string tableName, string keyColumn, string valueColumn)
        {
            var dictionary = new Dictionary<int, string>();

            using (var command = new NpgsqlCommand($"SELECT {keyColumn}, {valueColumn} FROM {tableName}", _connection))
            {
                _connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dictionary[reader.GetInt32(0)] = reader.GetString(1);
                    }
                }
                _connection.Close();
            }

            return dictionary;
        }

        private void HidePrimaryKeyColumn(string tableName)
        {
            var primaryKeyColumn = GetPrimaryKeyColumn(tableName);
            if (primaryKeyColumn != null && dataGridView1.Columns.Contains(primaryKeyColumn))
            {
                dataGridView1.Columns[primaryKeyColumn].Visible = false;
            }
        }

        private string GetPrimaryKeyColumn(string tableName)
        {
            if (tableName == "partner_products") return "sale_id";
            if (tableName == "products") return "product_id";
            if (tableName == "partners") return "partner_id";
            if (tableName == "material_types") return "type_id";
            if (tableName == "product_types") return "type_id";
            if (tableName == "materials") return "material_id";
            return null;
        }


        private void ReplaceForeignKeyColumns(string tableName)
        {
            if (tableName == "partner_products")
            {
                AddComboBoxColumn("product_id", "products");
                AddComboBoxColumn("partner_id", "partners");
            }
            else if (tableName == "products")
            {
                AddComboBoxColumn("product_type_id", "product_types");
            }
            else if (tableName == "materials")
            {
                AddComboBoxColumn("material_type_id", "material_types");
            }
        }


        private void AddComboBoxColumn(string columnName, string lookupTable)
        {
            if (!_dataTable.Columns.Contains(columnName)) return;

            if (dataGridView1.Columns.Contains(columnName))
                dataGridView1.Columns.Remove(columnName);

            var comboBoxColumn = new DataGridViewComboBoxColumn
            {
                Name = columnName,
                DataPropertyName = columnName,
                HeaderText = columnName.Replace("_id", ""),
                DataSource = new BindingSource(_lookupData[lookupTable], null),
                DisplayMember = "Value",
                ValueMember = "Key",
                FlatStyle = FlatStyle.Flat
            };

            dataGridView1.Columns.Add(comboBoxColumn);
        }

        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            var tableName = comboBox1.Text;

            switch (tableName)
            {
                case "partner_products":
                    SetDefaultForeignKeyValue(e.Row, "product_id", "products");
                    SetDefaultForeignKeyValue(e.Row, "partner_id", "partners");
                    e.Row.Cells["quantity"].Value = 1;
                    e.Row.Cells["sale_date"].Value = DateTime.Now;
                    break;
                case "products":
                    SetDefaultForeignKeyValue(e.Row, "product_type_id", "product_types");
                    e.Row.Cells["min_partner_price"].Value = 0.0m;
                    break;
                case "materials":
                    SetDefaultForeignKeyValue(e.Row, "material_type_id", "material_types");
                    e.Row.Cells["purchase_price"].Value = 0.0m;
                    e.Row.Cells["stock_quantity"].Value = 0;
                    break;
            }
        }

        private void SetDefaultForeignKeyValue(DataGridViewRow row, string columnName, string lookupTable)
        {
            if (_lookupData.ContainsKey(lookupTable) && _lookupData[lookupTable].Any())
            {
                row.Cells[columnName].Value = _lookupData[lookupTable].Keys.First();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                _adapter.Update(_dataTable);
                MessageBox.Show("Данные успешно сохранены в базу данных!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.IsNewRow) return;

            dataGridView1.Rows.Remove(dataGridView1.CurrentRow);
            MessageBox.Show("Строка удалена. Не забудьте сохранить изменения.");
        }
    }
}