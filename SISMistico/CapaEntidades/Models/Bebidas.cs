using System;
using System.Data;

namespace CapaEntidades.Models
{
    public class Bebidas
    {
        public Bebidas()
        {

        }

        public Bebidas(DataRow row)
        {
            try
            {
                this.Id_bebida = Convert.ToInt32(row["Id_bebida"]);
                this.Nombre_bebida = Convert.ToString(row["Nombre_bebida"]);
                this.Descripcion_bebida = Convert.ToString(row["Descripcion_bebida"]);
                this.Precio_bebida = Convert.ToDecimal(row["Precio_bebida"]);
                this.Imagen = Convert.ToString(row["Imagen"]);
                this.Id_tipo_bebida = Convert.ToInt32(row["Id_tipo_bebida"]);
                this.Estado = Convert.ToString(row["Estado"]);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message, null);
            }
        }

        public int Id_bebida { get; set; }

        public string Nombre_bebida { get; set; }

        public string Descripcion_bebida { get; set; }

        public decimal Precio_bebida { get; set; }

        public string Imagen { get; set; }

        public string RutaImagen { get; set; }

        public int Id_tipo_bebida { get; set; }

        public string Estado { get; set; }

        public event EventHandler OnError;
    }
}
