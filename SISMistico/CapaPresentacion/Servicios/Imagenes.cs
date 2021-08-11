using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Configuration;
using System.IO;
using CapaPresentacion.Properties;
using CapaPresentacion.Servicios;

namespace CapaPresentacion
{
    public class Imagenes
    { 
        public static Image ObtenerImagen(string nombre_imagen, out string ruta_destino)
        {
            ruta_destino = ConfigGeneral.Default.RutaImagenes;
            Image Imagen;
            try
            {
                if (File.Exists(Path.Combine(ruta_destino, nombre_imagen)))
                    Imagen = Image.FromFile(Path.Combine(ruta_destino, nombre_imagen));
                else
                    Imagen = Resources.NotFileCoffe;

            }
            catch (Exception)
            {
                Imagen = Image.FromFile($"ruta_destino AlmuerzoEspecial.jpg");
            }
            return Imagen;
        }
    }
}
