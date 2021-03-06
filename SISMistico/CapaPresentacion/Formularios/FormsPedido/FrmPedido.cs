namespace CapaPresentacion.Formularios.FormsPedido
{
    using CapaEntidades.Models;
    using CapaNegocio;
    using CapaPresentacion.Formularios.FormsClientes;
    using CapaPresentacion.Formularios.FormsPedido.Platos;
    using CapaPresentacion.Properties;
    using CapaPresentacion.Servicios;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public partial class FrmPedido : Form
    {
        public FrmPedido()
        {
            InitializeComponent();
            this.txtBusqueda.onKeyPress += TxtBusqueda_OnTextoKeyPress;
            this.btnPlatos.Click += BtnPlatos_Click;
            this.btnBebidas.Click += BtnBebidas_Click;
            this.btnSave.Click += BtnSave_Click;
            this.Load += FrmPedido_Load;
            this.btnSelectClient.Click += BtnSelectClient_Click;
        }

        private void BtnSelectClient_Click(object sender, EventArgs e)
        {
            FrmObservarClientes frm = new FrmObservarClientes
            {
                StartPosition = FormStartPosition.CenterScreen,
            };
            frm.OnClientSelected += Frm_OnClientSelected;
            frm.ShowDialog();
        }

        private void Frm_OnClientSelected(object sender, EventArgs e)
        {
            Clientes cliente = (Clientes)sender;
            this.ClienteSelected = cliente;
        }

        public event EventHandler OnPedidoSaveSuccess;

        private DialogResult Comprobacion()
        {
            FrmComprobacion frm = new FrmComprobacion
            {
                StartPosition = FormStartPosition.CenterScreen
            };
            frm.FormClosed += FrmComprobacion_FormClosed;
            frm.ShowDialog();
            return frm.DialogResult;
        }

        private void FrmComprobacion_FormClosed(object sender, FormClosedEventArgs e)
        {
            FrmComprobacion frm = (FrmComprobacion)sender;
            if (frm.DialogResult == DialogResult.OK)
            {
                DatosInicioSesion datos = DatosInicioSesion.GetInstancia();
                datos.EmpleadoClaveMaestra = frm.EmpleadoSelected;
            }
        }

        private bool Comprobaciones(out List<string> variablesPedido,
            out DataTable dtDetallePedidoPrint,
            out List<Detalle_ingredientes_pedido> listDetalleIngredientes)
        {
            //Asignar la tabla de los detalles
            dtDetallePedidoPrint = new DataTable("DetallePedido");
            //Asignar las variables del pedido, es decir los datos principales
            variablesPedido = new List<string>();
            //Asignar la lista de ingredientes del detalle del pedido
            listDetalleIngredientes = new List<Detalle_ingredientes_pedido>();

            if (this.EmpleadoSelected == null)
            {
                MensajeEspera.CloseForm();
                Mensajes.MensajeInformacion("No hay un empleado seleccionado");
                return false;
            }

            if (this.ClienteSelected == null)
            {
                MensajeEspera.CloseForm();
                Mensajes.MensajeInformacion("No hay un cliente seleccionado");
                return false;
            }

            if (this.MesaSelected == null)
            {
                MensajeEspera.CloseForm();
                Mensajes.MensajeInformacion("No hay una mesa seleccionada");
                return false;
            }

            string tipo_pedido;
            if (this.IsDomicilio)
                tipo_pedido = "DOMICILIO";
            else
                tipo_pedido = "MESA";

            variablesPedido = new List<string>
                {
                Convert.ToString(this.MesaSelected.Id_mesa),
                Convert.ToString(this.EmpleadoSelected.Id_empleado),
                Convert.ToString(this.ClienteSelected.Id_cliente),
                "0", tipo_pedido, "", this.numericClientes.Value.ToString()
                };


            //Comprobar si hay productos seleccionados
            if (this.ProductsAddSelected == null && !this.IsEditar)
            {
                MensajeEspera.CloseForm();
                Mensajes.MensajeInformacion("No hay productos seleccionados");
                return false;
            }

            dtDetallePedidoPrint.Columns.Add("Id_pedido", typeof(int));
            dtDetallePedidoPrint.Columns.Add("Id_tipo", typeof(int));
            dtDetallePedidoPrint.Columns.Add("Tipo", typeof(string));
            dtDetallePedidoPrint.Columns.Add("Nombre", typeof(string));
            dtDetallePedidoPrint.Columns.Add("Precio", typeof(decimal));
            dtDetallePedidoPrint.Columns.Add("Cantidad", typeof(int));
            dtDetallePedidoPrint.Columns.Add("Total", typeof(string));
            dtDetallePedidoPrint.Columns.Add("Observaciones", typeof(string));

            if (this.ProductsAddSelected != null)
            {
                foreach (ProductBinding pr in this.ProductsAddSelected)
                {
                    DataRow newRowPrint = dtDetallePedidoPrint.NewRow();

                    if (this.IsEditar)
                        newRowPrint["Id_pedido"] = this.Pedido.Id_pedido;
                    else
                        newRowPrint["Id_pedido"] = 0;

                    newRowPrint["Id_tipo"] = pr.Id_producto;
                    newRowPrint["Tipo"] = pr.Tipo_producto;
                    newRowPrint["Nombre"] = pr.Nombre;
                    newRowPrint["Precio"] = pr.Precio;
                    newRowPrint["Cantidad"] = pr.Cantidad;
                    newRowPrint["Total"] = pr.Cantidad * pr.Precio;
                    newRowPrint["Observaciones"] = pr.Observaciones;

                    //Agregamos la lista de detalles si es un plato
                    if (pr.Tipo_producto.Equals("PLATO"))
                    {
                        CapaEntidades.Models.Platos plato =
                            (CapaEntidades.Models.Platos)pr.Product;
                        if (plato.Plato_detallado.Equals("ACTIVO"))
                        {
                            if (pr.ProductDetalles != null)
                            {
                                StringBuilder info = new StringBuilder();
                                info.Append("-" + plato.Nombre_plato).Append(": ").Append(Environment.NewLine);

                                foreach (ProductDetalleBinding de in pr.ProductDetalles)
                                {
                                    Detalle_ingredientes_pedido detail = new Detalle_ingredientes_pedido
                                    {
                                        Id_ingrediente = de.Id_ingrediente,
                                        Ingrediente = de.Ingrediente,
                                        Id_pedido = de.Id_pedido,
                                        Id_tipo = pr.Id_producto,
                                    };

                                    info.Append(de.Ingrediente.Nombre_ingrediente).Append(Environment.NewLine);
                                    newRowPrint["Nombre"] = info.ToString();
                                    listDetalleIngredientes.Add(detail);
                                }
                            }
                        }
                    }

                    dtDetallePedidoPrint.Rows.Add(newRowPrint);
                }
            }
            else
                dtDetallePedidoPrint = null;

            return true;
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                MensajeEspera.ShowWait("Cargando...");
                if (this.Comprobaciones(out List<string> variablesPedido,
                                        out DataTable dtDetallePedidoPrint,
                                        out List<Detalle_ingredientes_pedido> listDetalleIngredientes))
                {
                    DatosInicioSesion datos = DatosInicioSesion.GetInstancia();

                    string rpta = "OK";
                    int id_pedido = 0;

                    if (!this.IsEditar)
                    {
                        rpta = NPedido.InsertarPedido(variablesPedido,
                           dtDetallePedidoPrint, out id_pedido, out DataTable dtDetallesCompleto);
                    }
                    else
                    {
                        id_pedido = this.Pedido.Id_pedido;
                        if (dtDetallePedidoPrint != null)
                        {
                            foreach (DataRow row in dtDetallePedidoPrint.Rows)
                            {
                                Detalle_pedido de = new Detalle_pedido(row);

                                //Encontrar la cantidad correcta
                                List<ProductBinding> find =
                                    this.ProductsSelected.Where(x => x.Id_producto == de.Id_tipo).ToList();
                                if (find.Count > 0)
                                {
                                    ProductBinding pr = find[0];
                                    de.Cantidad = pr.Cantidad;
                                }

                                rpta = NPedido.ActualizarDetallePedido(de, datos.EmpleadoClaveMaestra.Id_empleado, "");
                                if (!rpta.Equals("OK"))
                                {
                                    throw new Exception(rpta);
                                }
                            }
                        }
                    }

                    if (rpta.Equals("OK"))
                    {
                        //Cuando tengamos devuelta los detalles vamos a comprobar
                        //cuales de esos detalles hay que ingresarles el detalle de almuerzo
                        if (listDetalleIngredientes != null)
                        {
                            if (listDetalleIngredientes.Count > 0)
                            {
                                foreach (Detalle_ingredientes_pedido de in listDetalleIngredientes)
                                {
                                    //Asignar el id pedido que es igual para todos los detalles
                                    de.Id_pedido = id_pedido;
                                    de.Observaciones = string.Empty;
                                }

                                rpta = await NPedido.InsertarDetalleIngredientesPedido(listDetalleIngredientes);
                            }
                        }

                        if (!rpta.Equals("OK"))
                        {
                            Mensajes.MensajeInformacion("No se ingresaron detalles de platos, operación interrumpida");
                        }

                        if (!this.IsDomicilio)
                        {
                            FrmObservarMesas FrmObservarMesas = FrmObservarMesas.GetInstancia();
                            //Id_mesa = 0 error
                            //FrmObservarMesas.LiberarMesa(this.MesaSelected.Id_mesa);
                            FrmObservarMesas.CargarMesas();
                        }
                        else
                            this.OnPedidoSaveSuccess?.Invoke(this.Pedido, e);

                        if (this.IsEditar)
                        {
                            if (dtDetallePedidoPrint != null)
                            {
                                this.frmComandas.Id_pedido = id_pedido;
                                this.frmComandas.AsignarTablas(dtDetallePedidoPrint);
                            }
                        }
                        else
                        {
                            this.frmComandas.Id_pedido = id_pedido;
                            this.frmComandas.AsignarTablas();
                        }

                        if (this.chkPrintComandas.Checked)
                        {
                            this.frmComandas.ImprimirFactura(1);
                        }
                        MensajeEspera.CloseForm();
                        this.Close();
                    }
                    else
                        throw new Exception(rpta);
                }
                MensajeEspera.CloseForm();
            }
            catch (Exception ex)
            {
                MensajeEspera.CloseForm();
                Mensajes.MensajeErrorCompleto(this.Name, "BtnSave_Click",
                    "Hubo un error guardando el pedido",
                    ex.Message);
            }
        }

        private void TxtBusqueda_OnTextoKeyPress(object sender, KeyPressEventArgs e)
        {
            CustomTextBox txt = (CustomTextBox)sender;
            if (e.KeyChar == (int)Keys.Enter)
            {
                if (this.optionSelected.Equals("PLATOS"))
                {
                    this.LoadPlatos("NOMBRE", txt.Texto);
                }
                else
                {
                    this.LoadBebidas("NOMBRE", txt.Texto);
                }
            }
        }

        private void FrmPedido_Load(object sender, EventArgs e)
        {
            DialogResult dialog = this.Comprobacion();
            if (dialog == DialogResult.OK)
            {
                MensajeEspera.ShowWait("Cargando...");
                this.optionSelected = "PLATOS";
                this.LoadTipoPlatos("COMPLETO", "");

                if (this.ListTipoPlatos != null)
                    this.LoadPlatos("ID TIPO PLATO", this.ListTipoPlatos[0].Id_tipo_plato.ToString());

                if (this.frmComandas == null)
                    this.frmComandas = new FrmComandas();

                this.frmComandas.ObtenerReporte();

                if (this.IsEditar)
                {
                    this.LoadProductsSelected(this.ProductsSelected);
                }

                MensajeEspera.CloseForm();
            }
            else
                this.Close();
        }

        private void BtnBebidas_Click(object sender, EventArgs e)
        {
            this.optionSelected = "BEBIDAS";
            this.LoadTipoBebidas("COMPLETO", "");
        }

        private void BtnPlatos_Click(object sender, EventArgs e)
        {
            this.optionSelected = "PLATOS";
            this.LoadTipoPlatos("COMPLETO", "");
        }

        private void AddProduct(ProductBinding product,
            List<ProductDetalleBinding> detalles, bool isEditar)
        {
            //Si la lista de productos seleccionados está vacía la inicializamos
            if (this.ProductsSelected == null)
                this.ProductsSelected = new List<ProductBinding>();

            //Comprobar existencia del producto en la lista de ProductsSelected
            List<ProductBinding> productExiste =
                this.ProductsSelected.Where(x => x.Id_producto == product.Id_producto).ToList();
            if (productExiste.Count > 0)
            {
                /**Si existe el producto en la lista de productos haremos:
                 * 1- Verificar si es un plato
                 * 2- Obtener el plato desde el object de la entidad de producto
                 * 3- Si es un plato detallado, verificar si hay cambios en su detalle
                 * 4- Si hay cambios, agregar un item nuevo
                 * 5- Si no hay cambios agregar +1 al producto en la lista existente**/

                if (productExiste[0].Tipo_producto.Equals("PLATO"))
                {
                    CapaEntidades.Models.Platos plato =
                        (CapaEntidades.Models.Platos)productExiste[0].Product;
                    if (plato.Plato_detallado.Equals("ACTIVO"))
                    {
                        if (detalles == null)
                        {
                            Mensajes.MensajeInformacion("No se envío ningún detalle");
                            return;
                        }

                        if (productExiste[0].ProductDetalles == null)
                            productExiste[0].ProductDetalles = detalles;

                        bool isNew = false;
                        foreach (ProductDetalleBinding pr1 in detalles)
                        {
                            List<ProductDetalleBinding> find =
                                productExiste[0].ProductDetalles.Where(x => x.Id_ingrediente == pr1.Id_ingrediente).ToList();
                            //QUIERE DECIR QUE NO HAY COINCIDENCIAS
                            if (find.Count == 0)
                            {
                                isNew = true;
                                break;
                            }
                        }

                        if (isNew)
                        {
                            //Si es new agregamos uno nuevo
                            ProductBinding pr = new ProductBinding
                            {
                                Id_producto = product.Id_producto,
                                Nombre = product.Nombre,
                                Tipo_producto = product.Tipo_producto,
                                Precio = product.Precio,
                                Observaciones = string.Empty,
                                NombreImagen = product.NombreImagen,
                                ProductDetalles = detalles,
                                IsAddBD = true,
                                IsEditar = isEditar,
                                Cantidad = 1,
                                Product = plato,
                            };
                            this.ProductsSelected.Add(pr);
                        }
                        else
                            productExiste[0].Cantidad += 1;
                    }
                    else
                        productExiste[0].Cantidad += 1;
                }
                else
                    productExiste[0].Cantidad += 1;
            }
            else
            {
                //Si no existe agregamos un producto nuevo a la lista de vista

                product.IsAddBD = true;
                product.IsEditar = isEditar;
                product.Cantidad = 1;

                if (product.ProductDetalles == null)
                    product.ProductDetalles = detalles;

                this.ProductsSelected.Add(product);
            }

            //Comprobar existencia del producto en ProductsAddSelected
            List<ProductBinding> productExisteAdd = new List<ProductBinding>();

            if (this.ProductsAddSelected == null)
                this.ProductsAddSelected = new List<ProductBinding>();

            productExisteAdd =
                this.ProductsAddSelected.Where(x => x.Id_producto == product.Id_producto).ToList();
            if (productExisteAdd.Count > 0)
            {
                /**Si existe el producto en la lista de productos haremos:
                 * 1- Agregar +1 al producto en la lista existente**/
                //productExisteAdd[0].Cantidad += 1;
                //this.ProductsAddSelected.Add(productExiste[0]);
                //product.Cantidad += 1;
            }
            else
            {
                //Si no existe agregamos un producto nuevo
                product.IsAddBD = true;
                product.IsEditar = isEditar;
                product.Cantidad = 1;
                if (product.ProductDetalles == null)
                    product.ProductDetalles = detalles;
                this.ProductsAddSelected.Add(product);
            }

            //Cargar los productos desde la lista de seleccionados general
            this.LoadProductsSelected(this.ProductsSelected);
        }

        private void RemoveProduct(ProductBinding product)
        {
            DatosInicioSesion datos = DatosInicioSesion.GetInstancia();
            /**Si el producto IsEditar y IsAddBD es false
             * Significa que el producto venía desde la BD**/
            if (product.IsEditar && product.IsAddBD == false)
            {
                DialogResult dialog = this.Comprobacion();
                if (dialog == DialogResult.OK)
                {
                    Detalle_pedido detalle = new Detalle_pedido
                    {
                        Id_pedido = this.Pedido.Id_pedido,
                        Id_tipo = product.Id_producto,
                        Tipo = product.Tipo_producto,
                        Precio = product.Precio,
                        Cantidad = product.Cantidad - 1,
                        Observaciones = product.Observaciones == null ? string.Empty : product.Observaciones,
                    };

                    //Eliminar de la base de datos
                    string rpta = NPedido.ActualizarDetallePedido(detalle, datos.EmpleadoClaveMaestra.Id_empleado, "DELETE");
                    if (!rpta.Equals("OK"))
                    {
                        Mensajes.MensajeInformacion("Hubo un error actualizando el detalle del pedido en la bd");
                    }
                }
                else
                    return;
            }

            //Comprobar existencia del producto en la lista de ProductsSelected
            List<ProductBinding> productExiste =
                this.ProductsSelected.Where(x => x.Id_producto == product.Id_producto).ToList();
            if (productExiste.Count > 0)
            {
                /**Si existe el producto en la lista de productos haremos:
                 * 1- Comprobar cuánta cantidad tiene el producto en la lista existente
                 * si la cantidad es 1 se quitará de la lista, si la cantidad es > 0 
                 * se restará -1.**/

                if (productExiste[0].Cantidad == 1)
                {
                    this.ProductsSelected.Remove(productExiste[0]);
                }
                else
                    productExiste[0].Cantidad -= 1;
            }
            else
            {
                //Si no existe, es porque hay algún error
                Mensajes.MensajeInformacion("Hubo un error eliminando el producto, " +
                    "no se encuentra en al lista de productos seleccionados");
            }

            //Comprobar existencia del producto en ProductsAddSelected
            if (this.ProductsAddSelected != null &&
                this.ProductsAddSelected.Count > 0)
            {
                productExiste =
                    this.ProductsAddSelected.Where(x => x.Id_producto == product.Id_producto).ToList();
                if (productExiste.Count > 0)
                {
                    /**Si existe el producto en la lista de productos haremos:
                     * 1- Comprobar cuánta cantidad tiene el producto en la lista existente
                     * si la cantidad es 1 se quitará de la lista, si la cantidad es > 0 
                     * se restará -1.**/
                    if (productExiste[0].Cantidad == 1)
                    {
                        this.ProductsAddSelected.Remove(productExiste[0]);
                    }
                    //else
                    //    productExiste[0].Cantidad -= 1;
                }
            }

            this.LoadProductsSelected(this.ProductsSelected);
        }

        private void LoadProductsSelected(List<ProductBinding> products)
        {
            try
            {
                this.panelPedido.clearDataSource();
                StringBuilder info = new StringBuilder();
                info.Append("Fecha y hora: ").Append(DateTime.Now.ToLongDateString());
                info.Append(" " + DateTime.Now.ToLongTimeString()).Append(" | ");

                if (this.ClienteSelected.Id_cliente != 0)
                {
                    info.Append("Cliente: ").Append(this.ClienteSelected.Nombre_cliente).Append(" - ");
                    info.Append("Teléfono: ").Append(this.ClienteSelected.Telefono_cliente).Append(" - ");
                    info.Append("Dirección: ").Append(this.ClienteSelected.Direccion_cliente).Append(" - ");
                    info.Append("Referencia: ").Append(this.ClienteSelected.Referencia_ubicacion);

                    if (!string.IsNullOrEmpty(this.ClienteSelected.Otras_observaciones))
                        info.Append(" - Otras observaciones: ").Append(this.ClienteSelected.Otras_observaciones);

                    info.Append(" | ");
                }
                else
                    info.Append("Sin cliente seleccionado: ").Append(" | ");

                if (products != null)
                {
                    decimal subtotal = 0;
                    this.panelPedido.BackgroundImage = null;
                    List<UserControl> controls = new List<UserControl>();
                    foreach (ProductBinding pr in products)
                    {
                        subtotal += pr.Precio * pr.Cantidad;
                        ProductoItem productoItem = new ProductoItem
                        {
                            Product = pr,
                        };
                        productoItem.OnBtnAddClick += ProductoItem_OnBtnAddClick;
                        productoItem.OnBtnRemoveClick += ProductoItem_OnBtnRemoveClick;
                        controls.Add(productoItem);
                    }
                    info.Append("Subtotal: ").Append(subtotal.ToString("C"));
                    this.panelPedido.AddArrayControl(controls);

                    this.threadLoadImages = new Thread(new ThreadStart(() => LoadImagesProductsSelected()))
                    {
                        IsBackground = true
                    };
                    this.threadLoadImages.SetApartmentState(ApartmentState.STA);
                    this.threadLoadImages.Start();
                }
                else
                {
                    info.Append("Subtotal: ").Append(0.ToString("C"));
                    this.panelPedido.BackgroundImage = Resources.SIN_IMAGENES;
                    this.panelPedido.BackgroundImageLayout = ImageLayout.Center;
                }
                this.txtInfoPedido.Text = info.ToString();
            }
            catch (Exception ex)
            {
                Mensajes.MensajeErrorCompleto(this.Name, "LoadProductsSelected(List<ProductBinding> products)",
                    "Hubo un error al cargar los productos seleccionados", ex.Message);
            }
        }

        public void LoadPlatos(string tipo_busqueda, string texto_busqueda)
        {
            try
            {
                DataTable dtPlatos = NPlatos.BuscarPlatos(tipo_busqueda, texto_busqueda, "ACTIVO", out string rpta);
                this.panelResultados.clearDataSource();
                if (dtPlatos != null)
                {
                    this.panelResultados.BackgroundImage = null;
                    List<UserControl> controls = new List<UserControl>();
                    foreach (DataRow row in dtPlatos.Rows)
                    {
                        CapaEntidades.Models.Platos plato = new CapaEntidades.Models.Platos(row);

                        ProductBinding productBinding = new ProductBinding
                        {
                            Nombre = plato.Nombre_plato,
                            Id_producto = plato.Id_plato,
                            Tipo_producto = "PLATO",
                            Precio = plato.Precio_plato,
                            Observaciones = string.Empty,
                            Cantidad = 0,
                            Product = plato,
                            NombreImagen = plato.Imagen_plato,
                        };

                        ProductoItem productoItem = new ProductoItem
                        {
                            Product = productBinding,
                        };

                        productoItem.OnBtnAddClick += ProductoItem_OnBtnAddClick;

                        controls.Add(productoItem);
                    }
                    this.panelResultados.AddArrayControl(controls);

                    this.threadLoadImages = new Thread(new ThreadStart(() => LoadImages()))
                    {
                        IsBackground = true
                    };
                    this.threadLoadImages.SetApartmentState(ApartmentState.STA);
                    this.threadLoadImages.Start();
                }
                else
                {
                    this.panelResultados.BackgroundImage = Resources.SIN_IMAGENES;
                    this.panelResultados.BackgroundImageLayout = ImageLayout.Center;

                    if (!rpta.Equals("OK"))
                        throw new Exception(rpta);
                }
            }
            catch (Exception ex)
            {
                Mensajes.MensajeErrorCompleto(this.Name, "LoadPlatos(string tipo_busqueda, string texto_busqueda)",
                    "Hubo un error cargando los platos", ex.Message);
            }
        }

        public void LoadBebidas(string tipo_busqueda, string texto_busqueda)
        {
            try
            {
                MensajeEspera.ShowWait("Cargando...");
                DataTable dtBebidas = NBebidas.BuscarBebida(tipo_busqueda, texto_busqueda, "ACTIVO", out string rpta);
                this.panelResultados.clearDataSource();
                if (dtBebidas != null)
                {
                    this.panelResultados.BackgroundImage = null;
                    List<UserControl> controls = new List<UserControl>();
                    foreach (DataRow row in dtBebidas.Rows)
                    {
                        CapaEntidades.Models.Bebidas bebida = new CapaEntidades.Models.Bebidas(row);

                        ProductBinding productBinding = new ProductBinding
                        {
                            Nombre = bebida.Nombre_bebida,
                            Id_producto = bebida.Id_bebida,
                            Tipo_producto = "BEBIDA",
                            Precio = bebida.Precio_bebida,
                            Observaciones = string.Empty,
                            Cantidad = 0,
                            Product = bebida,
                            NombreImagen = bebida.Imagen,
                        };

                        ProductoItem productoItem = new ProductoItem
                        {
                            Product = productBinding,
                        };

                        productoItem.OnBtnAddClick += ProductoItem_OnBtnAddClick;

                        controls.Add(productoItem);
                    }
                    this.panelResultados.AddArrayControl(controls);

                    this.threadLoadImages = new Thread(new ThreadStart(() => LoadImages()))
                    {
                        IsBackground = true
                    };
                    this.threadLoadImages.SetApartmentState(ApartmentState.STA);
                    this.threadLoadImages.Start();
                }
                else
                {
                    MensajeEspera.CloseForm();

                    this.panelResultados.BackgroundImage = Resources.SIN_IMAGENES;
                    this.panelResultados.BackgroundImageLayout = ImageLayout.Center;

                    if (!rpta.Equals("OK"))
                        throw new Exception(rpta);
                }

                MensajeEspera.CloseForm();
            }
            catch (Exception ex)
            {
                MensajeEspera.CloseForm();

                Mensajes.MensajeErrorCompleto(this.Name, "LoadBebidas(string tipo_busqueda, string texto_busqueda)",
                    "Hubo un error cargando las bebidas", ex.Message);
            }
        }

        private void LoadImages()
        {
            foreach (UserControl control in this.panelResultados.controlsUser)
            {
                if (control is ProductoItem product)
                {
                    Image img;

                    string nombreimagen = product.Product.NombreImagen;
                    string rutaImagenes = ConfigGeneral.Default.RutaImagenes;

                    if (!string.IsNullOrEmpty(nombreimagen))
                    {
                        DirectoryInfo DirectoryInfo = new DirectoryInfo(rutaImagenes);
                        string destino = Path.Combine(DirectoryInfo.ToString(), nombreimagen);

                        if (File.Exists(destino))
                        {
                            img = Imagenes.ObtenerImagen(nombreimagen, out string _);
                        }
                        else
                        {
                            img = Resources.SIN_IMAGENES;
                        }

                        if (img != null)
                            product.ImageProduct = img;
                    }
                    else
                    {
                        img = Resources.SIN_IMAGENES;
                        product.ImageProduct = img;
                    }
                }
            }

            if (this.threadLoadImages.IsAlive)
                this.threadLoadImages.Interrupt();
        }

        private void LoadImagesProductsSelected()
        {
            if (this.ProductsSelected != null)
            {
                foreach (UserControl control in this.panelPedido.controlsUser)
                {
                    if (control is ProductoItem product)
                    {
                        Image img;
                        string nombreimagen = product.Product.NombreImagen;
                        string rutaImagenes = ConfigGeneral.Default.RutaImagenes;

                        DirectoryInfo DirectoryInfo = new DirectoryInfo(rutaImagenes);
                        string destino = Path.Combine(DirectoryInfo.ToString(), nombreimagen);

                        if (!string.IsNullOrEmpty(nombreimagen))
                        {
                            if (File.Exists(destino))
                            {
                                img = Imagenes.ObtenerImagen(nombreimagen, out string _);
                            }
                            else
                            {
                                img = Resources.SIN_IMAGENES;
                            }

                            if (img != null)
                                product.ImageProduct = img;
                        }
                        else
                        {
                            img = Resources.SIN_IMAGENES;
                            product.ImageProduct = img;
                        }
                    }
                }

                if (this.threadLoadImages.IsAlive)
                    this.threadLoadImages.Interrupt();
            }
        }

        private void ProductoItem_OnBtnRemoveClick(object sender, EventArgs e)
        {
            ProductBinding product = (ProductBinding)sender;
            this.RemoveProduct(product);
        }

        private void ProductoItem_OnBtnAddClick(object sender, EventArgs e)
        {
            //Obtenemos y guardamos el producto con sus detalles
            ProductBinding product = (ProductBinding)sender;
            if (product.Tipo_producto.Equals("PLATO"))
            {
                CapaEntidades.Models.Platos plato =
                    (CapaEntidades.Models.Platos)product.Product;
                if (plato.Plato_detallado.Equals("ACTIVO"))
                {
                    /**Como abrimos los detalles, se debe de generar el evento guardar 
                      * para agregarlo correctamente a la lista**/
                    bool isEnabledBebida = plato.Plato_carta.Equals("ACTIVO") ? true : false;

                    FrmDetallePedidoPlato frmDetallePedidoPlato = new FrmDetallePedidoPlato
                    {
                        StartPosition = FormStartPosition.CenterScreen,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        Product = product,
                        IsEnabledBebida = isEnabledBebida,
                    };
                    frmDetallePedidoPlato.OnBtnSaveClick += FrmDetallePedidoPlato_OnBtnSaveClick;
                    frmDetallePedidoPlato.ShowDialog();
                    return;
                }
            }

            //Si es diferente a plato, se guardará sin más.
            this.AddProduct(product, null, this.IsEditar);
        }

        private void FrmDetallePedidoPlato_OnBtnSaveClick(object sender, EventArgs e)
        {
            object[] objs = (object[])sender;
            //Obtenemos y guardamos el producto con sus detalles
            ProductBinding product = (ProductBinding)objs[0];
            List<ProductDetalleBinding> detalles = (List<ProductDetalleBinding>)objs[1];
            this.AddProduct(product, detalles, this.IsEditar);
        }

        public void LoadTipoPlatos(string tipo_busqueda, string texto_busqueda)
        {
            try
            {
                string rpta = "OK";
                DataTable dtTipos = NPlatos.BuscarTipoPlatos(tipo_busqueda, texto_busqueda);
                this.panelTipo.clearDataSource();
                this.ListTipoPlatos = null;
                if (dtTipos != null)
                {
                    this.ListTipoPlatos = new List<Tipo_platos>();
                    this.panelTipo.BackgroundImage = null;

                    List<UserControl> controls = new List<UserControl>();
                    foreach (DataRow row in dtTipos.Rows)
                    {
                        Tipo_platos tipo = new Tipo_platos(row);
                        this.ListTipoPlatos.Add(tipo);

                        TipoItem tipoItem = new TipoItem
                        {
                            Tipo = "PLATO",
                            TipoObject = tipo,
                            NombreTipo = tipo.Tipo_plato.ToUpper(),
                        };
                        tipoItem.OnBtnTipoClick += TipoItem_OnBtnTipoClick;
                        controls.Add(tipoItem);
                    }
                    this.panelTipo.AddArrayControl(controls);
                }
                else
                {
                    this.panelTipo.BackgroundImage = Resources.SIN_IMAGENES;
                    this.panelTipo.BackgroundImageLayout = ImageLayout.Center;

                    if (!rpta.Equals("OK"))
                        throw new Exception(rpta);
                }
            }
            catch (Exception ex)
            {
                Mensajes.MensajeErrorCompleto(this.Name, "LoadTipoPlatos(string tipo_busqueda, string texto_busqueda)",
                    "Hubo un error cargando los tipos de platos", ex.Message);
            }
        }

        public void LoadTipoBebidas(string tipo_busqueda, string texto_busqueda)
        {
            try
            {
                string rpta = "OK";
                DataTable dtTipos = NBebidas.BuscarTipoBebidas(tipo_busqueda, texto_busqueda);
                this.panelTipo.clearDataSource();
                this.ListTipoBebidas = null;
                if (dtTipos != null)
                {
                    this.ListTipoBebidas = new List<Tipo_bebidas>();
                    this.panelTipo.BackgroundImage = null;
                    List<UserControl> controls = new List<UserControl>();
                    foreach (DataRow row in dtTipos.Rows)
                    {
                        Tipo_bebidas tipo = new Tipo_bebidas(row);
                        this.ListTipoBebidas.Add(tipo);

                        TipoItem tipoItem = new TipoItem
                        {
                            Tipo = "BEBIDA",
                            TipoObject = tipo,
                            NombreTipo = tipo.Tipo_bebida.ToUpper(),
                        };
                        tipoItem.OnBtnTipoClick += TipoItem_OnBtnTipoClick;
                        controls.Add(tipoItem);
                    }
                    this.panelTipo.AddArrayControl(controls);
                }
                else
                {
                    this.panelTipo.BackgroundImage = Resources.SIN_IMAGENES;
                    this.panelTipo.BackgroundImageLayout = ImageLayout.Center;

                    if (!rpta.Equals("OK"))
                        throw new Exception(rpta);
                }
            }
            catch (Exception ex)
            {
                Mensajes.MensajeErrorCompleto(this.Name, "LoadTipoBebidas(string tipo_busqueda, string texto_busqueda)",
                    "Hubo un error cargando los tipos de bebidas", ex.Message);
            }
        }

        private void TipoItem_OnBtnTipoClick(object sender, EventArgs e)
        {
            TipoItem tipoItem = (TipoItem)sender;
            if (tipoItem.Tipo.Equals("BEBIDA"))
            {
                Tipo_bebidas tipo = (Tipo_bebidas)tipoItem.TipoObject;
                this.LoadBebidas("ID TIPO BEBIDA", tipo.Id_tipo_bebida.ToString());
            }
            else
            {
                Tipo_platos tipo = (Tipo_platos)tipoItem.TipoObject;
                this.LoadPlatos("ID TIPO PLATO", tipo.Id_tipo_plato.ToString());
            }
        }

        private void AsignarDatos(Pedidos pedido)
        {
            this.ClienteSelected = pedido.Cliente;
            this.EmpleadoSelected = pedido.Empleado;
            this.MesaSelected = pedido.Mesa;

            this.lblMesero.Text = "Mesero/Empleado " + pedido.Empleado.Nombre_empleado;
            this.lblTitulo.Text = "Adicionar/Remover productos del pedido número " + pedido.Id_pedido;
            this.numericClientes.Value = pedido.CantidadClientes;
            this.IsEditar = true;

            //Obtener el detalle del pedido
            DataTable dtDatosPrincipales =
                NPedido.BuscarPedidosYDetalle("ID PEDIDO Y DETALLE", pedido.Id_pedido.ToString(),
                out DataTable dtDetalles,
                out DataTable dtDetallePlatosPedido, out string rpta);

            if (dtDetalles != null)
            {
                List<ProductBinding> products = new List<ProductBinding>();
                products = (from DataRow dr in dtDetalles.Rows
                            select new ProductBinding
                            {
                                Nombre = Convert.ToString(dr["Nombre"]),
                                Precio = Convert.ToDecimal(dr["Precio"]),
                                Cantidad = Convert.ToInt32(dr["Cantidad"]),
                                Id_producto = Convert.ToInt32(dr["Id_tipo"]),
                                Tipo_producto = Convert.ToString(dr["Tipo"]),
                                Product = this.GetProduct(Convert.ToString(dr["Tipo"]),
                                Convert.ToInt32(dr["Id_tipo"]),
                                dtDetallePlatosPedido,
                                out string nombre_imagen,
                                out List<ProductDetalleBinding> detalles),
                                NombreImagen = nombre_imagen,
                                ProductDetalles = detalles,
                                IsEditar = true,
                                IsAddBD = false,
                            }).ToList();

                this.ProductsSelected = new List<ProductBinding>();
                this.ProductsSelected.AddRange(products);
            }
        }

        private Ingredientes GetIngrediente(int id_ingrediente)
        {
            DataTable dtIngrediente =
                NIngredientes.BuscarIngredientes("ID INGREDIENTE", id_ingrediente.ToString(), out string rpta);
            if (dtIngrediente != null)
            {
                return new Ingredientes(dtIngrediente.Rows[0]);
            }
            else
                return null;
        }

        private object GetProduct(string tipo_producto,
            int id_producto,
            DataTable dtDetallesPlatosPedido,
            out string nombre_imagen,
            out List<ProductDetalleBinding> detalles)
        {
            detalles = new List<ProductDetalleBinding>();
            nombre_imagen = string.Empty;
            if (tipo_producto.Equals("PLATO"))
            {
                DataTable dtPlato =
                    NPlatos.BuscarPlatos("ID PLATO", id_producto.ToString(), "ACTIVO", out string rpta);
                if (dtPlato != null)
                {
                    CapaEntidades.Models.Platos plato =
                        new CapaEntidades.Models.Platos(dtPlato.Rows[0]);
                    nombre_imagen = plato.Imagen_plato;
                    if (plato.Plato_detallado.Equals("ACTIVO"))
                    {
                        DataRow[] find =
                            dtDetallesPlatosPedido.Select(string.Format("Id_tipo = {0}", plato.Id_plato));
                        if (find.Length > 0)
                        {
                            detalles = new List<ProductDetalleBinding>();
                            detalles = (from DataRow dr in find
                                        select new ProductDetalleBinding
                                        {
                                            Id_detalle_ingrediente_pedido =
                                            Convert.ToInt32(dr["Id_detalle_ingrediente_pedido"]),
                                            Id_pedido =
                                            Convert.ToInt32(dr["Id_pedido"]),
                                            Id_tipo =
                                            Convert.ToInt32(dr["Id_tipo"]),
                                            Id_ingrediente =
                                            Convert.ToInt32(dr["Id_ingrediente"]),
                                            Ingrediente =
                                            this.GetIngrediente(Convert.ToInt32(dr["Id_ingrediente"])),
                                            Observaciones =
                                            Convert.ToString(dr["Observaciones"]),
                                        }).ToList();
                        }
                    }
                    return plato;
                }
                else
                    return null;
            }
            else if (tipo_producto.Equals("BEBIDA"))
            {
                DataTable dtBebida =
                    NBebidas.BuscarBebida("ID BEBIDA", id_producto.ToString(), "ACTIVO", out string rpta);
                if (dtBebida != null)
                {
                    CapaEntidades.Models.Bebidas bebida =
                        new CapaEntidades.Models.Bebidas(dtBebida.Rows[0]);
                    nombre_imagen = bebida.Imagen;
                    return bebida;
                }
                else
                    return null;
            }
            else
                return null;
        }

        #region PROPIEDADES Y VARIABLES
        private string _informacion;
        private bool _isEditar;
        private string _tipo_servicio;
        private string optionSelected = string.Empty;
        private Pedidos _pedido;
        public Pedidos Pedido
        {
            get => _pedido;
            set
            {
                _pedido = value;
                this.AsignarDatos(value);
            }
        }

        Thread threadLoadImages;
        //PoperContainer container;

        public Clientes ClienteSelected { get; set; }
        public Empleados EmpleadoSelected { get; set; }
        public Mesas MesaSelected { get; set; }

        public FrmComandas frmComandas;

        public List<Tipo_platos> ListTipoPlatos { get; set; }
        public List<Tipo_bebidas> ListTipoBebidas { get; set; }
        public List<ProductBinding> ProductsSelected { get; set; }
        public List<ProductBinding> ProductsAddSelected { get; set; }
        public string Informacion
        {
            get => _informacion;
            set
            {
                _informacion = value;
                this.txtInfoPedido.Text = value;
            }
        }
        public bool IsEditar
        {
            get => _isEditar;
            set
            {
                _isEditar = value;
                this.numericClientes.Visible = false;
                this.gbNumClientes.Visible = false;
            }
        }
        public bool IsDomicilio { get; set; }
        public int Numero_mesa { get; set; }
        public string Tipo_servicio
        {
            get => _tipo_servicio;
            set
            {
                _tipo_servicio = value;
                if (value.Equals("DOMICILIO"))                
                    this.lblTitulo.Text = "Nuevo domicilio";
                else
                    this.lblTitulo.Text = "Nuevo pedido para la mesa " + Numero_mesa;
            }
        }

        #endregion
    }
}
