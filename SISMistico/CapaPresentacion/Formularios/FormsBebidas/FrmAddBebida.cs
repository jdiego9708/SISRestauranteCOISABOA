using CapaEntidades.Models;
using CapaNegocio;
using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CapaPresentacion.Formularios.FormsBebidas
{
    public partial class FrmAddBebida : Form
    {
        public FrmAddBebida()
        {
            InitializeComponent();
            this.txtPrecio.KeyPress += TxtPrecio_KeyPress;
            this.btnGuardar.Click += BtnGuardar_Click;
            this.btnCancelar.Click += BtnCancelar_Click;
            this.Load += FrmAddBebida_Load;
        }

        private void FrmAddBebida_Load(object sender, EventArgs e)
        {
            if (!this.IsEditar)
                this.listaTipoBebidas =
    LlenarListas.LlenarListaTipoBebidas(this.listaTipoBebidas);
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private bool Comprobaciones(out Bebidas bebida)
        {
            if (this.IsEditar)
                bebida = this.Bebida;
            else
                bebida = new Bebidas();

            if (string.IsNullOrEmpty(this.txtNombre.Text))
            {
                Mensajes.MensajeInformacion("Verifique el campo nombre");
                return false;
            }

            if (string.IsNullOrEmpty(this.txtPrecio.Text))
            {
                Mensajes.MensajeInformacion("Verifique el campo precio");
                return false;
            }
            else
            {
                if (!decimal.TryParse(this.txtPrecio.Text, out decimal precio))
                {
                    Mensajes.MensajeInformacion("Verifique el campo precio deben ser solo numeros");
                    return false;
                }
                else
                    bebida.Precio_bebida = precio;
            }

            if (!int.TryParse(Convert.ToString(this.listaTipoBebidas.SelectedValue), out int id_tipo_bebida))
            {
                Mensajes.MensajeInformacion("Verifique el campo tipo de bebida");
                return false;
            }
            else
                bebida.Id_tipo_bebida = id_tipo_bebida;

            bebida.Nombre_bebida = this.txtNombre.Text;
            bebida.Descripcion_bebida = this.txtDescripcion.Text;
            bebida.Estado = "ACTIVO";
            bebida.Imagen = 
                string.IsNullOrEmpty(this.uploadImage1.Nombre_imagen) ? 
                "SIN IMAGEN" : this.uploadImage1.Nombre_imagen;
            bebida.RutaImagen =
                this.uploadImage1.Ruta_origen;

            return true;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                string rpta;

                if (this.Comprobaciones(out Bebidas bebida))
                {
                    this.Bebida = bebida;

                    if (this.IsEditar)
                    {
                        rpta = NBebidas.EditarBebida(new List<string>
                        {
                            this.Bebida.Id_bebida.ToString(),
                            this.Bebida.Nombre_bebida,
                            this.Bebida.Precio_bebida.ToString("N2"),
                            this.Bebida.Imagen,
                            this.Bebida.Id_tipo_bebida.ToString(),
                        });
                    }
                    else
                    {
                        rpta = NBebidas.InsertarBebida(new List<string>
                        {
                            this.Bebida.Nombre_bebida,
                            this.Bebida.Descripcion_bebida,
                            this.Bebida.Precio_bebida.ToString("N2"),
                            this.Bebida.Imagen,
                            this.Bebida.Id_tipo_bebida.ToString(),
                        }, out int id_bebida);
                        this.Bebida.Id_bebida = id_bebida;
                    }

                    if (rpta.Equals("OK"))
                    {
                        //Guardar imágenes
                        rpta = ArchivosAdjuntos.GuardarArchivo(this.Bebida.Id_bebida, "rutaImages",
                            this.uploadImage1.Nombre_imagen,
                            this.Bebida.RutaImagen);

                        if(rpta.Equals("OK"))
                            Mensajes.MensajeOkForm("Se actualizó la bebida correctamente");
                        else
                            Mensajes.MensajeOkForm("Se actualizó la bebida correctamente pero no se guardó su imagen");

                        this.Close();
                    }
                    else
                        throw new Exception($"Hubo un error actualizando la bebida, detalles: {rpta}");
                }
            }
            catch (Exception ex)
            {
                Mensajes.MensajeErrorCompleto(this.Name, "BtnGuardar_Click",
                    "Hubo un error guardando las bebidas", ex.Message);
            }
        }

        private void TxtPrecio_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsDigit(e.KeyChar))
            {
                e.Handled = false;
            }
            else if (char.IsControl(e.KeyChar))
            {
                e.Handled = false;
            }
            else if (char.IsLetter(e.KeyChar))
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = true;
            }
        }

        private bool _isEditar;

        private Bebidas _bebida;

        public Bebidas Bebida
        {
            get => _bebida;
            set
            {
                _bebida = value;
                this.AsignarDatos(value);
            }
        }

        public bool IsEditar
        {
            get => _isEditar;
            set
            {
                _isEditar = value;
                this.Text = "Editar una bebida";
            }
        }

        private void AsignarDatos(Bebidas bebida)
        {
            if (bebida != null)
            {
                this.listaTipoBebidas =
                    LlenarListas.LlenarListaTipoBebidas(this.listaTipoBebidas);

                this.txtNombre.Text = bebida.Nombre_bebida;
                this.txtDescripcion.Text = bebida.Descripcion_bebida;
                this.txtPrecio.Text = bebida.Precio_bebida.ToString();
                this.listaTipoBebidas.SelectedValue = bebida.Id_tipo_bebida;
                this.uploadImage1.AsignarImagen(bebida.Imagen);
            }
        }
    }
}
