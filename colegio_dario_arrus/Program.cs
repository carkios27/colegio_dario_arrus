using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;


// ============================================================
// MESA DE PARTES DIGITAL - I.E. Darío Arrús Cuestas N.° 5011
// Punto de entrada: carga los datos guardados y abre el inicio de sesión.
// ============================================================
public static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Archivos.Cargar();
        Application.Run(new FormLogin());
    }
}

// ============================================================
// DATOS: arreglos paralelos. La misma posición en todos los
// arreglos corresponde al mismo trámite.
// ============================================================
public static class Datos
{
    public const int Max = 2000;   // el colegio tiene ~1200 documentos y recibe ~50 al día

    public static int[] Expedientes = new int[Max];
    public static string[] Solicitantes = new string[Max];
    public static string[] Dnis = new string[Max];
    public static string[] Tipos = new string[Max];
    public static string[] Destinos = new string[Max];     // cuenta de quien debe atender el trámite
    public static string[] Estados = new string[Max];      // Recibido, Derivado o Atendido
    public static string[] Fechas = new string[Max];       // fecha y hora de registro
    public static string[] RecibidoPor = new string[Max];  // quién recibió el documento
    public static string[] Movimientos = new string[Max];  // fecha y hora del último movimiento
    public static string[] Telefonos = new string[Max];
    public static string[] Parentescos = new string[Max];
    public static string[] Motivos = new string[Max];
    public static string[] Alumnos = new string[Max];      // nombre del alumno en la justificación

    public static int Contador = 0;

    public static readonly string[] ParentescosValidos =
        { "Madre", "Padre", "Tío(a)", "Abuelo(a)", "Hermano(a)", "Apoderado(a)", "Otro" };

    public static readonly string[] TiposValidos =
        { "justificacion", "vacante", "certificado", "constancia" };
}

// ============================================================
// VALIDACIONES: funciones que devuelven true/false.
// Si algo está mal, dejan el mensaje en UltimoError para que la
// ventana lo muestre.
// ============================================================
public static class Validaciones
{
    public static string UltimoError = "";

    // El nombre no puede estar vacío y solo lleva letras y espacios.
    public static bool ValidarNombre(string nombre)
    {
        if (nombre == "")
        {
            UltimoError = "Error: el nombre no puede estar vacío.";
            return false;
        }

        for (int i = 0; i < nombre.Length; i++)
        {
            if (!char.IsLetter(nombre[i]) && nombre[i] != ' ')
            {
                UltimoError = "Error: el nombre solo debe contener letras.";
                return false;
            }
        }
        return true;
    }

    // El DNI debe tener exactamente 8 dígitos numéricos.
    public static bool ValidarDNI(string dni)
    {
        if (dni.Length != 8)
        {
            UltimoError = "Error: el DNI debe tener 8 dígitos.";
            return false;
        }

        for (int i = 0; i < dni.Length; i++)
        {
            if (!char.IsDigit(dni[i]))
            {
                UltimoError = "Error: el DNI solo debe contener números.";
                return false;
            }
        }
        return true;
    }

    // El teléfono debe tener 9 dígitos numéricos y empezar con 9 (celular).
    public static bool ValidarTelefono(string telefono)
    {
        if (telefono.Length != 9)
        {
            UltimoError = "Error: el teléfono debe tener 9 dígitos.";
            return false;
        }

        for (int i = 0; i < telefono.Length; i++)
        {
            if (!char.IsDigit(telefono[i]))
            {
                UltimoError = "Error: el teléfono solo debe contener números.";
                return false;
            }
        }

        if (telefono[0] != '9')
        {
            UltimoError = "Error: el número de celular debe empezar con 9.";
            return false;
        }
        return true;
    }

    // Búsqueda lineal de un texto dentro de un arreglo (devuelve la posición o -1).
    public static int BuscarOpcion(string[] opciones, string texto)
    {
        string buscado = Normalizar(texto);
        for (int i = 0; i < opciones.Length; i++)
        {
            if (Normalizar(opciones[i]) == buscado)
                return i;
        }
        return -1;
    }

    // Quita espacios, pasa a minúsculas y elimina tildes.
    public static string Normalizar(string texto)
    {
        string t = texto.Trim().ToLower();
        t = t.Replace('á', 'a').Replace('é', 'e').Replace('í', 'i')
             .Replace('ó', 'o').Replace('ú', 'u');
        return t;
    }

    // Evita que el texto rompa el formato del archivo (el separador es '|').
    public static string LimpiarTexto(string texto)
    {
        return texto.Replace('|', ' ').Trim();
    }

    public static string FechaActual()
    {
        return DateTime.Now.ToString("dd/MM/yyyy HH:mm");
    }
}

// ============================================================
// USUARIOS: mesa de partes, director, coordinador y profesores.
// ============================================================
public static class Usuarios
{
    // Las 7 primeras cuentas reciben trámites; la última (mesa) los registra.
    public static readonly string[] Cuentas =
        { "director", "coordinador", "prof1", "prof2", "prof3", "prof4", "prof5", "mesa" };

    public static readonly string[] Claves =
        { "dir2026", "coord2026", "prof1234", "prof2345", "prof3456", "prof4567", "prof5678", "mesa2026" };

    public static readonly string[] Cargos =
        { "Director", "Coordinador", "Profesor", "Profesor", "Profesor", "Profesor", "Profesor", "Encargado" };

    public static readonly string[] Nombres =
        { "Luis Salazar", "Marta Vega", "Carmen Ruiz", "Jorge Paredes",
          "Elena Castro", "Daniel Rojas", "Sofía León", "Mesa de Partes" };

    public const int TotalDestinatarios = 7;

    public static int Actual = -1;   // posición del usuario con sesión iniciada (-1 = nadie)

    public static int BuscarCuenta(string cuenta)
    {
        return Validaciones.BuscarOpcion(Cuentas, cuenta);
    }

    public static string Etiqueta(int pos)
    {
        return Cargos[pos] + " " + Nombres[pos];
    }

    public static string EtiquetaDeCuenta(string cuenta)
    {
        int pos = BuscarCuenta(cuenta);
        if (pos == -1)
            return cuenta;
        return Etiqueta(pos);
    }

    // Mesa de partes, director y coordinador pueden ver todos los trámites.
    public static bool EsDirectivo()
    {
        return Actual != -1 && Cargos[Actual] != "Profesor";
    }

    // Solo la mesa de partes registra trámites nuevos.
    public static bool EsMesa()
    {
        return Actual != -1 && Cargos[Actual] == "Encargado";
    }

    // Devuelve true si el usuario y la contraseña son correctos.
    public static bool IniciarSesion(string cuenta, string clave)
    {
        int pos = BuscarCuenta(cuenta);
        if (pos != -1 && Claves[pos] == clave)
        {
            Actual = pos;
            return true;
        }
        return false;
    }

    public static void CerrarSesion()
    {
        Actual = -1;
    }
}

// ============================================================
// TRAMITES: lógica del sistema. No usa pantalla ni consola:
// recibe datos y devuelve resultados (números o textos).
// ============================================================
public static class Tramites
{
    private static int SiguienteExpediente()
    {
        int mayor = 0;
        for (int i = 0; i < Datos.Contador; i++)
        {
            if (Datos.Expedientes[i] > mayor)
                mayor = Datos.Expedientes[i];
        }
        return mayor + 1;
    }

    // Búsqueda lineal: devuelve la posición del expediente en los arreglos, o -1 si no existe.
    public static int BuscarPosicion(int numero)
    {
        for (int i = 0; i < Datos.Contador; i++)
        {
            if (Datos.Expedientes[i] == numero)
                return i;
        }
        return -1;
    }

    // Mesa, director y coordinador ven todos; el profesor solo los dirigidos a él.
    public static bool PuedeVer(int i)
    {
        if (Usuarios.EsDirectivo())
            return true;
        return Datos.Destinos[i] == Usuarios.Cuentas[Usuarios.Actual];
    }

    // Un trámite está pendiente mientras no tenga el estado "Atendido".
    public static bool EstaPendiente(int i)
    {
        return Datos.Estados[i] != "Atendido";
    }

    public static int ContarPendientes()
    {
        int total = 0;
        for (int i = 0; i < Datos.Contador; i++)
        {
            if (PuedeVer(i) && EstaPendiente(i))
                total++;
        }
        return total;
    }

    public static int ContarAtendidos()
    {
        int total = 0;
        for (int i = 0; i < Datos.Contador; i++)
        {
            if (PuedeVer(i) && !EstaPendiente(i))
                total++;
        }
        return total;
    }

    // Cantidad de trámites pendientes dirigidos a una cuenta.
    public static int ContarPendientesDe(string cuenta)
    {
        int total = 0;
        for (int i = 0; i < Datos.Contador; i++)
        {
            if (PuedeVer(i) && EstaPendiente(i) && Datos.Destinos[i] == cuenta)
                total++;
        }
        return total;
    }

    // Guarda el trámite. Devuelve el número de expediente, o -1 si no hay espacio.
    public static int Registrar(string solicitante, string parentesco, string dni, string telefono,
                                int posTipo, string motivo, string alumno, int posDestino, string encargado)
    {
        if (Datos.Contador >= Datos.Max)
            return -1;

        int pos = Datos.Contador;
        int expediente = SiguienteExpediente();
        string ahora = Validaciones.FechaActual();

        Datos.Expedientes[pos] = expediente;
        Datos.Solicitantes[pos] = solicitante;
        Datos.Dnis[pos] = dni;
        Datos.Tipos[pos] = Datos.TiposValidos[posTipo];
        Datos.Destinos[pos] = Usuarios.Cuentas[posDestino];
        Datos.Estados[pos] = "Recibido";
        Datos.Fechas[pos] = ahora;
        Datos.RecibidoPor[pos] = encargado;
        Datos.Movimientos[pos] = ahora;
        Datos.Telefonos[pos] = telefono;
        Datos.Parentescos[pos] = parentesco;
        Datos.Motivos[pos] = motivo;
        Datos.Alumnos[pos] = alumno;
        Datos.Contador++;

        Archivos.Guardar();
        return expediente;
    }

    // Una línea de texto con los datos principales de un trámite (para las listas).
    public static string Linea(int i)
    {
        return "Exp. " + Datos.Expedientes[i] + " | " + Datos.Solicitantes[i] + " | " + Datos.Tipos[i] +
               " | Dirigido a: " + Usuarios.EtiquetaDeCuenta(Datos.Destinos[i]) +
               " | Estado: " + Datos.Estados[i] + " | " + Datos.Fechas[i];
    }

    // Versión corta de la línea (para imprimir el reporte).
    public static string LineaCorta(int i)
    {
        return "Exp. " + Datos.Expedientes[i] + " | " + Datos.Solicitantes[i] + " | " + Datos.Tipos[i] +
               " | " + Datos.Estados[i] + " | " + Usuarios.EtiquetaDeCuenta(Datos.Destinos[i]);
    }

    // Todos los datos de un trámite (para el usuario del colegio).
    public static string Detalle(int i)
    {
        string nl = Environment.NewLine;
        string texto = "Solicitante: " + Datos.Solicitantes[i] + " (" + Datos.Parentescos[i] + " del alumno)" + nl;
        texto += "DNI: " + Datos.Dnis[i] + nl;
        texto += "Teléfono: " + Datos.Telefonos[i] + nl;
        texto += "Trámite: " + Datos.Tipos[i] + nl;
        if (Datos.Tipos[i] == "justificacion")
        {
            texto += "Motivo: " + Datos.Motivos[i] + nl;
            texto += "Nombre y apellido del alumno: " + Datos.Alumnos[i] + nl;
        }
        texto += "Recibido por: " + Datos.RecibidoPor[i] + nl;
        texto += "Fecha de registro: " + Datos.Fechas[i] + nl;
        texto += "Dirigido a: " + Usuarios.EtiquetaDeCuenta(Datos.Destinos[i]) + nl;
        texto += "Estado: " + Datos.Estados[i] + nl;
        texto += "Último movimiento: " + Datos.Movimientos[i];
        return texto;
    }

    // Búsqueda para el usuario con sesión iniciada.
    public static string Buscar(int numero)
    {
        int i = BuscarPosicion(numero);
        if (i == -1)
            return "Expediente no encontrado.";
        if (!PuedeVer(i))
            return "Este expediente no está dirigido a usted.";
        return Detalle(i);
    }

    // Consulta del padre/apoderado: necesita el N° de expediente y el DNI del solicitante.
    // Solo muestra datos de seguimiento (no teléfono ni datos de otras personas).
    public static string Consultar(int numero, string dni)
    {
        int i = BuscarPosicion(numero);
        if (i == -1 || Datos.Dnis[i] != dni)
            return "No se encontró un trámite con esos datos.";

        string nl = Environment.NewLine;
        string texto = "Trámite: " + Datos.Tipos[i] + nl;
        texto += "Estado: " + Datos.Estados[i] + nl;
        texto += "Ubicación actual: " + Usuarios.EtiquetaDeCuenta(Datos.Destinos[i]) + nl;
        texto += "Fecha de registro: " + Datos.Fechas[i] + nl;
        texto += "Último movimiento: " + Datos.Movimientos[i];
        return texto;
    }

    // Cambia el destino de un trámite. Devuelve el mensaje del resultado.
    public static string Derivar(int numero, int posDestino)
    {
        int i = BuscarPosicion(numero);
        if (i == -1)
            return "Expediente no encontrado.";
        if (Datos.Destinos[i] != Usuarios.Cuentas[Usuarios.Actual])
            return "Solo puede derivar trámites dirigidos a usted.";
        if (!EstaPendiente(i))
            return "Este trámite ya fue atendido y no se puede derivar.";
        if (posDestino == Usuarios.Actual)
            return "Error: el trámite ya está dirigido a usted.";

        Datos.Destinos[i] = Usuarios.Cuentas[posDestino];
        Datos.Estados[i] = "Derivado";
        Datos.Movimientos[i] = Validaciones.FechaActual();
        Archivos.Guardar();
        return "Trámite derivado a: " + Usuarios.Etiqueta(posDestino);
    }

    // Marca un trámite como atendido. Devuelve el mensaje del resultado.
    public static string MarcarAtendido(int numero)
    {
        int i = BuscarPosicion(numero);
        if (i == -1)
            return "Expediente no encontrado.";
        if (Datos.Destinos[i] != Usuarios.Cuentas[Usuarios.Actual])
            return "Solo puede atender trámites dirigidos a usted.";
        if (!EstaPendiente(i))
            return "Este trámite ya estaba atendido.";

        Datos.Estados[i] = "Atendido";
        Datos.Movimientos[i] = Validaciones.FechaActual();
        Archivos.Guardar();
        return "Trámite " + numero + " marcado como atendido.";
    }

    // Texto del cargo de recepción (se imprime y se entrega al padre/apoderado).
    // Devuelve "" si el expediente no existe o el usuario no puede verlo.
    public static string TextoCargo(int numero)
    {
        int i = BuscarPosicion(numero);
        if (i == -1 || !PuedeVer(i))
            return "";

        string nl = Environment.NewLine;
        string texto = "CARGO DE RECEPCIÓN DE TRÁMITE" + nl + nl;
        texto += "N° de expediente: " + Datos.Expedientes[i] + nl;
        texto += "Fecha de registro: " + Datos.Fechas[i] + nl;
        texto += "Solicitante: " + Datos.Solicitantes[i] + nl;
        texto += "DNI: " + Datos.Dnis[i] + nl;
        texto += "Parentesco con el alumno: " + Datos.Parentescos[i] + nl;
        texto += "Teléfono: " + Datos.Telefonos[i] + nl;
        texto += "Tipo de trámite: " + Datos.Tipos[i] + nl;
        if (Datos.Tipos[i] == "justificacion")
        {
            texto += "Motivo: " + Datos.Motivos[i] + nl;
            texto += "Nombre y apellido del alumno: " + Datos.Alumnos[i] + nl;
        }
        texto += "Dirigido a: " + Usuarios.EtiquetaDeCuenta(Datos.Destinos[i]) + nl;
        texto += "Recibido por: " + Datos.RecibidoPor[i] + nl + nl;
        texto += "Con este cargo puede consultar el estado de su trámite" + nl;
        texto += "indicando su número de expediente y su DNI." + nl + nl + nl;
        texto += "______________________________" + nl;
        texto += "Firma y sello de Mesa de Partes";
        return texto;
    }

    // Texto de la constancia de atención. Devuelve "" si el expediente no existe,
    // el usuario no puede verlo o el trámite todavía está pendiente.
    public static string TextoConstancia(int numero)
    {
        int i = BuscarPosicion(numero);
        if (i == -1 || !PuedeVer(i) || EstaPendiente(i))
            return "";

        string nl = Environment.NewLine;
        string atendio = Usuarios.EtiquetaDeCuenta(Datos.Destinos[i]);
        string texto = "CONSTANCIA DE ATENCIÓN DE TRÁMITE" + nl + nl;
        texto += "Se deja constancia de que el trámite de tipo \"" + Datos.Tipos[i] + "\"," + nl;
        texto += "presentado por " + Datos.Solicitantes[i] + " (DNI " + Datos.Dnis[i] + ")," + nl;
        texto += "con N° de expediente " + Datos.Expedientes[i] + ", fue ATENDIDO por" + nl;
        texto += atendio + " el " + Datos.Movimientos[i] + "." + nl + nl;
        texto += "Fecha de registro del trámite: " + Datos.Fechas[i] + nl + nl;
        texto += "Se expide la presente a solicitud del interesado." + nl + nl + nl;
        texto += "______________________________" + nl;
        texto += atendio;
        return texto;
    }

    // Reporte de pendientes y atendidos (según lo que el usuario puede ver).
    public static string TextoReporte()
    {
        string nl = Environment.NewLine;
        int pendientes = ContarPendientes();
        int atendidos = ContarAtendidos();

        string texto = "REPORTE DE TRÁMITES - MESA DE PARTES DIGITAL" + nl;
        texto += "I.E. Darío Arrús Cuestas N.° 5011" + nl;
        texto += "Generado: " + Validaciones.FechaActual() + nl;
        texto += "Usuario: " + Usuarios.Etiqueta(Usuarios.Actual) + nl + nl;

        texto += "RESUMEN" + nl;
        texto += "Pendientes: " + pendientes + nl;
        texto += "Atendidos: " + atendidos + nl;
        texto += "Total: " + (pendientes + atendidos) + nl + nl;

        texto += "PENDIENTES POR DESTINATARIO" + nl;
        for (int u = 0; u < Usuarios.TotalDestinatarios; u++)
        {
            int cantidad = ContarPendientesDe(Usuarios.Cuentas[u]);
            if (cantidad > 0)
                texto += Usuarios.Etiqueta(u) + ": " + cantidad + nl;
        }
        texto += nl;

        texto += "LISTA DE PENDIENTES (" + pendientes + ")" + nl;
        for (int i = 0; i < Datos.Contador; i++)
        {
            if (PuedeVer(i) && EstaPendiente(i))
                texto += LineaCorta(i) + nl;
        }
        texto += nl;

        texto += "LISTA DE ATENDIDOS (" + atendidos + ")" + nl;
        for (int i = 0; i < Datos.Contador; i++)
        {
            if (PuedeVer(i) && !EstaPendiente(i))
                texto += LineaCorta(i) + nl;
        }
        return texto;
    }

    // Ordena de mayor a menor número de expediente (más recientes primero) con burbuja.
    // Al ser arreglos paralelos, cada intercambio mueve todos los datos del trámite.
    public static void Ordenar()
    {
        for (int i = 0; i < Datos.Contador - 1; i++)
        {
            for (int j = 0; j < Datos.Contador - 1 - i; j++)
            {
                if (Datos.Expedientes[j] < Datos.Expedientes[j + 1])
                {
                    int expediente = Datos.Expedientes[j];
                    Datos.Expedientes[j] = Datos.Expedientes[j + 1];
                    Datos.Expedientes[j + 1] = expediente;
                    Intercambiar(Datos.Solicitantes, j);
                    Intercambiar(Datos.Dnis, j);
                    Intercambiar(Datos.Tipos, j);
                    Intercambiar(Datos.Destinos, j);
                    Intercambiar(Datos.Estados, j);
                    Intercambiar(Datos.Fechas, j);
                    Intercambiar(Datos.RecibidoPor, j);
                    Intercambiar(Datos.Movimientos, j);
                    Intercambiar(Datos.Telefonos, j);
                    Intercambiar(Datos.Parentescos, j);
                    Intercambiar(Datos.Motivos, j);
                    Intercambiar(Datos.Alumnos, j);
                }
            }
        }
        Archivos.Guardar();
    }

    private static void Intercambiar(string[] arreglo, int j)
    {
        string temp = arreglo[j];
        arreglo[j] = arreglo[j + 1];
        arreglo[j + 1] = temp;
    }
}

// ============================================================
// ARCHIVOS: guarda y carga los trámites en datos/tramites.txt
// (un trámite por línea, campos separados por '|') y guarda
// el reporte en datos/reporte.txt.
// ============================================================
public static class Archivos
{
    private static readonly string Carpeta =
        Path.Combine(AppContext.BaseDirectory, "datos");

    private static readonly string Ruta =
        Path.Combine(Carpeta, "tramites.txt");

    public static void Guardar()
    {
        try
        {
            Directory.CreateDirectory(Carpeta);

            string[] lineas = new string[Datos.Contador];
            for (int i = 0; i < Datos.Contador; i++)
            {
                lineas[i] = string.Join("|",
                    Datos.Expedientes[i],
                    Datos.Solicitantes[i],
                    Datos.Dnis[i],
                    Datos.Tipos[i],
                    Datos.Destinos[i],
                    Datos.Estados[i],
                    Datos.Fechas[i],
                    Datos.RecibidoPor[i],
                    Datos.Movimientos[i],
                    Datos.Telefonos[i],
                    Datos.Parentescos[i],
                    Datos.Motivos[i],
                    Datos.Alumnos[i]);
            }
            File.WriteAllLines(Ruta, lineas);
        }
        catch (IOException ex)
        {
            MessageBox.Show("Error al guardar el archivo: " + ex.Message);
        }
    }

    public static void Cargar()
    {
        Datos.Contador = 0;

        if (!File.Exists(Ruta))
            return;

        try
        {
            string[] lineas = File.ReadAllLines(Ruta);
            for (int i = 0; i < lineas.Length && Datos.Contador < Datos.Max; i++)
            {
                string[] campos = lineas[i].Split('|');
                int exp;
                if ((campos.Length != 12 && campos.Length != 13) || !int.TryParse(campos[0], out exp))
                    continue; // línea dañada: se ignora

                if (Usuarios.BuscarCuenta(campos[4]) == -1)
                    continue; // destinatario inexistente

                int pos = Datos.Contador;
                Datos.Expedientes[pos] = exp;
                Datos.Solicitantes[pos] = campos[1];
                Datos.Dnis[pos] = campos[2];
                Datos.Tipos[pos] = campos[3];
                Datos.Destinos[pos] = campos[4];
                Datos.Estados[pos] = campos[5];
                Datos.Fechas[pos] = campos[6];
                Datos.RecibidoPor[pos] = campos[7];
                Datos.Movimientos[pos] = campos[8];
                Datos.Telefonos[pos] = campos[9];
                Datos.Parentescos[pos] = campos[10];
                Datos.Motivos[pos] = campos[11];
                Datos.Alumnos[pos] = campos.Length == 13 ? campos[12] : "No registrado";
                Datos.Contador++;
            }
        }
        catch (IOException ex)
        {
            MessageBox.Show("Error al leer el archivo: " + ex.Message);
        }
    }

    // Guarda el texto del reporte en un archivo y devuelve la ruta (o "" si falló).
    public static string GuardarReporte(string texto)
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            string rutaReporte = Path.Combine(Carpeta, "reporte.txt");
            File.WriteAllText(rutaReporte, texto);
            return rutaReporte;
        }
        catch (IOException ex)
        {
            MessageBox.Show("Error al guardar el reporte: " + ex.Message);
            return "";
        }
    }
}

// ============================================================
// INTERFAZ: funciones que crean los controles (etiquetas, cajas,
// botones, listas desplegables, tarjetas) para no repetir código.
// ============================================================
public static class Interfaz
{
    public static Label CrearEtiqueta(string texto, int x, int y)
    {
        Label etiqueta = new Label();
        etiqueta.Text = texto;
        etiqueta.Left = x;
        etiqueta.Top = y;
        etiqueta.AutoSize = true;
        return etiqueta;
    }

    public static Label CrearTitulo(string texto, int x, int y)
    {
        Label titulo = CrearEtiqueta(texto, x, y);
        titulo.Font = new Font("Segoe UI", 18, FontStyle.Bold);
        titulo.ForeColor = Color.FromArgb(31, 56, 100);
        return titulo;
    }

    public static TextBox CrearCaja(int x, int y, int ancho)
    {
        TextBox caja = new TextBox();
        caja.Left = x;
        caja.Top = y;
        caja.Width = ancho;
        return caja;
    }

    public static Button CrearBoton(string texto, int x, int y, int ancho)
    {
        Button boton = new Button();
        boton.Text = texto;
        boton.Left = x;
        boton.Top = y;
        boton.Width = ancho;
        boton.Height = 32;
        return boton;
    }

    public static ComboBox CrearCombo(int x, int y, int ancho)
    {
        ComboBox combo = new ComboBox();
        combo.DropDownStyle = ComboBoxStyle.DropDownList;   // solo se puede elegir de la lista
        combo.Left = x;
        combo.Top = y;
        combo.Width = ancho;
        return combo;
    }

    // Botón del menú lateral (fondo azul, letras blancas).
    public static Button CrearBotonMenu(string texto, int y)
    {
        Button boton = new Button();
        boton.Text = texto;
        boton.Left = 0;
        boton.Top = y;
        boton.Width = 220;
        boton.Height = 48;
        boton.FlatStyle = FlatStyle.Flat;
        boton.FlatAppearance.BorderSize = 0;
        boton.BackColor = Color.FromArgb(31, 56, 100);
        boton.ForeColor = Color.White;
        boton.Font = new Font("Segoe UI", 11);
        boton.TextAlign = ContentAlignment.MiddleLeft;
        boton.Padding = new Padding(20, 0, 0, 0);
        return boton;
    }

    // Tarjeta de color con un título y un número grande (el Label del número lo recibe ya creado).
    public static Panel CrearTarjeta(string titulo, int x, int y, Color color, Label numero)
    {
        Panel tarjeta = new Panel();
        tarjeta.Left = x;
        tarjeta.Top = y;
        tarjeta.Width = 230;
        tarjeta.Height = 110;
        tarjeta.BackColor = color;

        Label lblTitulo = CrearEtiqueta(titulo, 15, 12);
        lblTitulo.ForeColor = Color.White;
        lblTitulo.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        tarjeta.Controls.Add(lblTitulo);

        numero.Text = "0";
        numero.AutoSize = true;
        numero.Left = 15;
        numero.Top = 42;
        numero.ForeColor = Color.White;
        numero.Font = new Font("Segoe UI", 28, FontStyle.Bold);
        tarjeta.Controls.Add(numero);

        return tarjeta;
    }
}

// ============================================================
// IMPRESIÓN: muestra la vista previa de un documento de texto
// y permite imprimirlo (o guardarlo como PDF con la impresora
// "Microsoft Print to PDF").
// ============================================================
public static class Impresion
{
    private static string[] lineas;
    private static int lineaActual;
    private static float tamanoLetra;

    public static void Imprimir(string titulo, string texto, float tamano)
    {
        lineas = texto.Split('\n');
        lineaActual = 0;
        tamanoLetra = tamano;

        PrintDocument documento = new PrintDocument();
        documento.DocumentName = titulo;
        documento.PrintPage += DocumentoPrintPage;

        PrintPreviewDialog vista = new PrintPreviewDialog();
        vista.Document = documento;
        vista.Width = 900;
        vista.Height = 700;
        vista.ShowDialog();
    }

    // Se ejecuta una vez por cada página que se va a dibujar.
    private static void DocumentoPrintPage(object sender, PrintPageEventArgs e)
    {
        Font fuenteTitulo = new Font("Arial", 14, FontStyle.Bold);
        Font fuente = new Font("Consolas", tamanoLetra);

        float x = e.MarginBounds.Left;
        float y = e.MarginBounds.Top;

        e.Graphics.DrawString("I.E. Darío Arrús Cuestas N.° 5011", fuenteTitulo, Brushes.Black, x, y);
        y += 45;

        float alto = fuente.GetHeight(e.Graphics) + 4;
        while (lineaActual < lineas.Length && y < e.MarginBounds.Bottom - alto)
        {
            e.Graphics.DrawString(lineas[lineaActual].TrimEnd('\r'), fuente, Brushes.Black, x, y);
            y += alto;
            lineaActual++;
        }

        e.HasMorePages = lineaActual < lineas.Length;   // true = falta otra página
    }
}

// ============================================================
// PANTALLA DE INICIO DE SESIÓN (máximo 3 intentos)
// También permite a los padres consultar su trámite.
// ============================================================
public class FormLogin : Form
{
    private TextBox txtUsuario;
    private TextBox txtClave;
    private int intentos = 0;

    public FormLogin()
    {
        Text = "Mesa de Partes Digital";
        ClientSize = new Size(460, 450);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.White;

        Controls.Add(Interfaz.CrearTitulo("MESA DE PARTES DIGITAL", 40, 30));
        Controls.Add(Interfaz.CrearEtiqueta("I.E. Darío Arrús Cuestas N.° 5011", 42, 78));

        Controls.Add(Interfaz.CrearEtiqueta("Usuario:", 42, 135));
        txtUsuario = Interfaz.CrearCaja(42, 160, 376);
        Controls.Add(txtUsuario);

        Controls.Add(Interfaz.CrearEtiqueta("Contraseña:", 42, 200));
        txtClave = Interfaz.CrearCaja(42, 225, 376);
        txtClave.UseSystemPasswordChar = true;   // muestra puntos en vez de letras
        Controls.Add(txtClave);

        Button btnIngresar = Interfaz.CrearBoton("Ingresar", 42, 275, 376);
        btnIngresar.Height = 40;
        btnIngresar.BackColor = Color.FromArgb(31, 56, 100);
        btnIngresar.ForeColor = Color.White;
        btnIngresar.Click += BtnIngresar_Click;
        Controls.Add(btnIngresar);

        Button btnConsulta = Interfaz.CrearBoton("Consulta de trámite (padres de familia)", 42, 330, 376);
        btnConsulta.Click += BtnConsulta_Click;
        Controls.Add(btnConsulta);

        Button btnSalir = Interfaz.CrearBoton("Salir", 42, 375, 376);
        btnSalir.Click += BtnSalir_Click;
        Controls.Add(btnSalir);

        AcceptButton = btnIngresar;   // Enter = Ingresar
    }

    private void BtnIngresar_Click(object sender, EventArgs e)
    {
        intentos++;

        if (Usuarios.IniciarSesion(txtUsuario.Text.Trim(), txtClave.Text))
        {
            intentos = 0;
            txtClave.Clear();

            Hide();                                   // oculta el inicio de sesión
            FormPrincipal principal = new FormPrincipal();
            principal.ShowDialog();                   // el programa sigue aquí cuando se cierra la ventana
            Usuarios.CerrarSesion();
            Show();                                   // vuelve a mostrar el inicio de sesión
            return;
        }

        if (intentos >= 3)
        {
            MessageBox.Show("Demasiados intentos fallidos. El programa se cerrará.");
            Close();
            return;
        }

        MessageBox.Show("Usuario o contraseña incorrectos. Intento " + intentos + " de 3.");
        txtClave.Clear();
    }

    private void BtnConsulta_Click(object sender, EventArgs e)
    {
        FormConsulta consulta = new FormConsulta();
        consulta.ShowDialog();
    }

    private void BtnSalir_Click(object sender, EventArgs e)
    {
        Close();
    }
}

// ============================================================
// CONSULTA DE TRÁMITE PARA PADRES: número de expediente + DNI
// ============================================================
public class FormConsulta : Form
{
    private TextBox txtExp;
    private TextBox txtDni;
    private Label lblResultado;

    public FormConsulta()
    {
        Text = "Consulta de trámite";
        ClientSize = new Size(520, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        Controls.Add(Interfaz.CrearTitulo("Consultar mi trámite", 20, 15));
        Controls.Add(Interfaz.CrearEtiqueta("Ingrese el N° de expediente de su cargo y el DNI del solicitante.", 22, 62));

        Controls.Add(Interfaz.CrearEtiqueta("N° de expediente:", 22, 105));
        txtExp = Interfaz.CrearCaja(180, 102, 150);
        Controls.Add(txtExp);

        Controls.Add(Interfaz.CrearEtiqueta("DNI del solicitante:", 22, 141));
        txtDni = Interfaz.CrearCaja(180, 138, 150);
        Controls.Add(txtDni);

        Button btnConsultar = Interfaz.CrearBoton("Consultar", 180, 180, 150);
        btnConsultar.Click += BtnConsultar_Click;
        Controls.Add(btnConsultar);

        lblResultado = Interfaz.CrearEtiqueta("", 22, 230);
        lblResultado.AutoSize = false;
        lblResultado.Width = 476;
        lblResultado.Height = 150;
        lblResultado.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(lblResultado);

        AcceptButton = btnConsultar;
    }

    private void BtnConsultar_Click(object sender, EventArgs e)
    {
        int numero;
        if (!int.TryParse(txtExp.Text, out numero) || numero <= 0)
        {
            MessageBox.Show("Error: ingrese un número de expediente válido.");
            return;
        }

        lblResultado.Text = Tramites.Consultar(numero, txtDni.Text.Trim());
    }
}

// ============================================================
// VENTANA PRINCIPAL DEL SISTEMA (pantalla completa)
// Menú lateral + secciones: Inicio, Registrar trámite (solo mesa
// de partes), Bandeja de trámites y Reportes.
// ============================================================
public class FormPrincipal : Form
{
    private Panel contenido;
    private Panel pnlInicio;
    private Panel pnlRegistro;
    private Panel pnlBandeja;
    private Panel pnlReportes;
    private Panel pnlCertificados;

    // Sección Inicio
    private Label lblTotal;
    private Label lblPendientes;
    private Label lblAtendidos;
    private ListBox listaRecientes;

    // Sección Registrar trámite
    private TextBox txtNombre;
    private ComboBox cboParentesco;
    private TextBox txtOtro;
    private TextBox txtDni;
    private TextBox txtTelefono;
    private ComboBox cboTipo;
    private TextBox txtMotivo;
    private Label lblAlumnoTramite;
    private TextBox txtAlumnoTramite;
    private ComboBox cboDestinoRegistro;
    private TextBox txtEncargado;

    // Sección Bandeja
    private Label lblConteo;
    private ComboBox cboFiltro;
    private ListBox lista;
    private TextBox txtExp;
    private ComboBox cboDestino;

    // posLista[k] = posición en los arreglos del trámite que se muestra en la fila k de la lista
    private int[] posLista = new int[Datos.Max];
    private int totalLista = 0;

    // Sección Reportes
    private TextBox txtReporte;

    // Sección de certificados escolares (solo director)
    private TextBox txtAlumnoCertificado;
    private TextBox txtDniCertificado;
    private TextBox txtGradoCertificado;
    private TextBox txtAnioCertificado;
    private ComboBox cboTipoCertificado;

    public FormPrincipal()
    {
        Text = "Mesa de Partes Digital - I.E. Darío Arrús Cuestas N.° 5011";
        ClientSize = new Size(1150, 680);
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        ConstruirMenu();

        contenido = new Panel();
        contenido.Left = 220;
        contenido.Top = 0;
        contenido.Width = 930;
        contenido.Height = 680;
        contenido.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Controls.Add(contenido);

        pnlInicio = CrearSeccion();
        pnlRegistro = CrearSeccion();
        pnlBandeja = CrearSeccion();
        pnlReportes = CrearSeccion();
        pnlCertificados = CrearSeccion();

        ConstruirInicio();
        ConstruirRegistro();
        ConstruirBandeja();
        ConstruirReportes();
        if (Usuarios.Actual == 0)
            ConstruirCertificados();

        ActualizarInicio();
        Mostrar(pnlInicio);

        WindowState = FormWindowState.Maximized;   // pantalla completa
    }

    // ---------------------------------------------------------
    // Construcción de la pantalla
    // ---------------------------------------------------------
    private void ConstruirMenu()
    {
        Panel menu = new Panel();
        menu.Left = 0;
        menu.Top = 0;
        menu.Width = 220;
        menu.Height = 680;
        menu.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        menu.BackColor = Color.FromArgb(31, 56, 100);
        Controls.Add(menu);

        Label lblColegio = Interfaz.CrearEtiqueta("I.E. N.° 5011" + Environment.NewLine + "Darío Arrús Cuestas", 20, 22);
        lblColegio.ForeColor = Color.White;
        lblColegio.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        menu.Controls.Add(lblColegio);

        Label lblUsuario = Interfaz.CrearEtiqueta(Usuarios.Etiqueta(Usuarios.Actual), 20, 75);
        lblUsuario.ForeColor = Color.FromArgb(200, 215, 240);
        menu.Controls.Add(lblUsuario);

        int y = 120;

        Button btnInicio = Interfaz.CrearBotonMenu("Inicio", y);
        btnInicio.Click += BtnInicio_Click;
        menu.Controls.Add(btnInicio);
        y += 50;

        if (Usuarios.EsMesa())   // solo la mesa de partes registra trámites
        {
            Button btnRegistro = Interfaz.CrearBotonMenu("Ingresar trámite", y);
            btnRegistro.Click += BtnRegistro_Click;
            menu.Controls.Add(btnRegistro);
            y += 50;
        }

        Button btnBandeja = Interfaz.CrearBotonMenu("Bandeja de trámites", y);
        btnBandeja.Click += BtnBandeja_Click;
        menu.Controls.Add(btnBandeja);
        y += 50;

        Button btnReportes = Interfaz.CrearBotonMenu("Reportes", y);
        btnReportes.Click += BtnReportes_Click;
        menu.Controls.Add(btnReportes);
        y += 50;

        if (Usuarios.Actual == 0)
        {
            Button btnCertificados = Interfaz.CrearBotonMenu("Certificados", y);
            btnCertificados.Click += BtnCertificados_Click;
            menu.Controls.Add(btnCertificados);
        }

        Button btnSalir = Interfaz.CrearBotonMenu("Cerrar sesión", 610);
        btnSalir.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnSalir.Click += BtnCerrarSesion_Click;
        menu.Controls.Add(btnSalir);
    }

    private Panel CrearSeccion()
    {
        Panel seccion = new Panel();
        seccion.Dock = DockStyle.Fill;
        seccion.BackColor = Color.White;
        seccion.Visible = false;
        contenido.Controls.Add(seccion);
        return seccion;
    }

    private void ConstruirInicio()
    {
        pnlInicio.Controls.Add(Interfaz.CrearTitulo("Panel de inicio", 30, 25));
        pnlInicio.Controls.Add(Interfaz.CrearEtiqueta("Bienvenido(a), " + Usuarios.Etiqueta(Usuarios.Actual), 33, 75));

        lblTotal = new Label();
        lblPendientes = new Label();
        lblAtendidos = new Label();
        pnlInicio.Controls.Add(Interfaz.CrearTarjeta("Total de trámites", 30, 115, Color.FromArgb(31, 56, 100), lblTotal));
        pnlInicio.Controls.Add(Interfaz.CrearTarjeta("Pendientes", 280, 115, Color.FromArgb(230, 126, 34), lblPendientes));
        pnlInicio.Controls.Add(Interfaz.CrearTarjeta("Atendidos", 530, 115, Color.FromArgb(39, 174, 96), lblAtendidos));

        pnlInicio.Controls.Add(Interfaz.CrearEtiqueta("Trámites pendientes (máximo 8)", 33, 250));
        listaRecientes = new ListBox();
        listaRecientes.Left = 30;
        listaRecientes.Top = 278;
        listaRecientes.Width = 870;
        listaRecientes.Height = 330;
        listaRecientes.HorizontalScrollbar = true;
        listaRecientes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pnlInicio.Controls.Add(listaRecientes);
    }

    private void ConstruirRegistro()
    {
        pnlRegistro.Controls.Add(Interfaz.CrearTitulo("Ingresar trámite y destinatario", 30, 25));
        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta(
            "Complete los datos y elija el cargo al que debe dirigirse el documento.", 33, 62));

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("Nombre del solicitante:", 33, 88));
        txtNombre = Interfaz.CrearCaja(260, 85, 360);
        pnlRegistro.Controls.Add(txtNombre);

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("Parentesco con el alumno:", 33, 130));
        cboParentesco = Interfaz.CrearCombo(260, 127, 360);
        for (int i = 0; i < Datos.ParentescosValidos.Length; i++)
            cboParentesco.Items.Add(Datos.ParentescosValidos[i]);
        cboParentesco.SelectedIndex = 0;
        pnlRegistro.Controls.Add(cboParentesco);

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("Si eligió \"Otro\", especifique:", 33, 172));
        txtOtro = Interfaz.CrearCaja(260, 169, 360);
        pnlRegistro.Controls.Add(txtOtro);

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("DNI (8 dígitos):", 33, 214));
        txtDni = Interfaz.CrearCaja(260, 211, 360);
        pnlRegistro.Controls.Add(txtDni);

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("Teléfono (9 dígitos):", 33, 256));
        txtTelefono = Interfaz.CrearCaja(260, 253, 360);
        pnlRegistro.Controls.Add(txtTelefono);

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("Tipo de trámite:", 33, 298));
        cboTipo = Interfaz.CrearCombo(260, 295, 360);
        for (int i = 0; i < Datos.TiposValidos.Length; i++)
            cboTipo.Items.Add(Datos.TiposValidos[i]);
        cboTipo.SelectedIndex = 0;
        pnlRegistro.Controls.Add(cboTipo);

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("Motivo (solo justificación):", 33, 340));
        txtMotivo = Interfaz.CrearCaja(260, 337, 360);
        pnlRegistro.Controls.Add(txtMotivo);

        lblAlumnoTramite = Interfaz.CrearEtiqueta("Nombre y apellido del alumno:", 33, 382);
        pnlRegistro.Controls.Add(lblAlumnoTramite);
        txtAlumnoTramite = Interfaz.CrearCaja(260, 379, 360);
        pnlRegistro.Controls.Add(txtAlumnoTramite);

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("Dirigido a:", 33, 424));
        cboDestinoRegistro = Interfaz.CrearCombo(260, 421, 360);
        for (int i = 0; i < Usuarios.TotalDestinatarios; i++)
            cboDestinoRegistro.Items.Add(Usuarios.Etiqueta(i));
        pnlRegistro.Controls.Add(cboDestinoRegistro);

        pnlRegistro.Controls.Add(Interfaz.CrearEtiqueta("Recibe el documento:", 33, 466));
        txtEncargado = Interfaz.CrearCaja(260, 463, 360);
        pnlRegistro.Controls.Add(txtEncargado);

        Button btnRegistrar = Interfaz.CrearBoton("Registrar trámite", 260, 515, 170);
        btnRegistrar.Click += BtnRegistrar_Click;
        pnlRegistro.Controls.Add(btnRegistrar);

        Button btnLimpiar = Interfaz.CrearBoton("Limpiar", 445, 515, 120);
        btnLimpiar.Click += BtnLimpiar_Click;
        pnlRegistro.Controls.Add(btnLimpiar);

        cboTipo.SelectedIndexChanged += CboTipo_Changed;
        ActualizarCampoAlumno();
    }

    private void ConstruirBandeja()
    {
        pnlBandeja.Controls.Add(Interfaz.CrearTitulo("Bandeja de trámites", 30, 25));

        lblConteo = Interfaz.CrearEtiqueta("", 33, 75);
        pnlBandeja.Controls.Add(lblConteo);

        pnlBandeja.Controls.Add(Interfaz.CrearEtiqueta("Mostrar:", 520, 75));
        cboFiltro = Interfaz.CrearCombo(585, 71, 150);
        cboFiltro.Items.Add("Todos");
        cboFiltro.Items.Add("Pendientes");
        cboFiltro.Items.Add("Atendidos");
        cboFiltro.SelectedIndex = 0;
        pnlBandeja.Controls.Add(cboFiltro);

        lista = new ListBox();
        lista.Left = 30;
        lista.Top = 105;
        lista.Width = 870;
        lista.Height = 300;
        lista.HorizontalScrollbar = true;
        lista.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pnlBandeja.Controls.Add(lista);

        // Panel inferior con los botones de acción (queda pegado abajo)
        Panel acciones = new Panel();
        acciones.Left = 0;
        acciones.Width = 930;
        acciones.Height = 175;
        acciones.Dock = DockStyle.Bottom;
        pnlBandeja.Controls.Add(acciones);

        // Mantiene la lista por encima de la franja de acciones al maximizar la ventana.
        pnlBandeja.Resize += delegate (object sender, EventArgs e)
        {
            lista.Height = Math.Max(100, pnlBandeja.ClientSize.Height - lista.Top - acciones.Height - 10);
        };

        acciones.Controls.Add(Interfaz.CrearEtiqueta("N° de expediente:", 33, 18));
        txtExp = Interfaz.CrearCaja(160, 15, 90);
        acciones.Controls.Add(txtExp);

        Button btnDetalle = Interfaz.CrearBoton("Ver detalle", 265, 12, 120);
        btnDetalle.Click += BtnDetalle_Click;
        acciones.Controls.Add(btnDetalle);

        Button btnAtender = Interfaz.CrearBoton("Marcar atendido", 395, 12, 130);
        btnAtender.Click += BtnAtender_Click;
        acciones.Controls.Add(btnAtender);

        Button btnCargo = Interfaz.CrearBoton("Imprimir cargo", 535, 12, 130);
        btnCargo.Click += BtnCargo_Click;
        acciones.Controls.Add(btnCargo);

        Button btnConstancia = Interfaz.CrearBoton("Imprimir constancia", 675, 12, 160);
        btnConstancia.Click += BtnConstancia_Click;
        acciones.Controls.Add(btnConstancia);

        acciones.Controls.Add(Interfaz.CrearEtiqueta("Derivar a:", 33, 68));
        cboDestino = Interfaz.CrearCombo(160, 65, 250);
        for (int i = 0; i < Usuarios.TotalDestinatarios; i++)
            cboDestino.Items.Add(Usuarios.Etiqueta(i));
        acciones.Controls.Add(cboDestino);

        Button btnDerivar = Interfaz.CrearBoton("Derivar trámite", 425, 62, 130);
        btnDerivar.Click += BtnDerivar_Click;
        acciones.Controls.Add(btnDerivar);

        Button btnOrdenar = Interfaz.CrearBoton("Ordenar (más recientes primero)", 565, 62, 230);
        btnOrdenar.Click += BtnOrdenar_Click;
        acciones.Controls.Add(btnOrdenar);

        // Los eventos se conectan al final, cuando todos los controles ya existen
        cboFiltro.SelectedIndexChanged += CboFiltro_Changed;
        lista.SelectedIndexChanged += Lista_Changed;
    }

    private void ConstruirReportes()
    {
        pnlReportes.Controls.Add(Interfaz.CrearTitulo("Reportes", 30, 25));

        Button btnGenerar = Interfaz.CrearBoton("Generar reporte", 30, 75, 160);
        btnGenerar.Click += BtnGenerar_Click;
        pnlReportes.Controls.Add(btnGenerar);

        Button btnImprimir = Interfaz.CrearBoton("Imprimir reporte", 205, 75, 160);
        btnImprimir.Click += BtnImprimirReporte_Click;
        pnlReportes.Controls.Add(btnImprimir);

        Button btnGuardar = Interfaz.CrearBoton("Guardar en archivo", 380, 75, 170);
        btnGuardar.Click += BtnGuardarReporte_Click;
        pnlReportes.Controls.Add(btnGuardar);

        txtReporte = new TextBox();
        txtReporte.Left = 30;
        txtReporte.Top = 125;
        txtReporte.Width = 870;
        txtReporte.Height = 510;
        txtReporte.Multiline = true;
        txtReporte.ReadOnly = true;
        txtReporte.ScrollBars = ScrollBars.Vertical;
        txtReporte.Font = new Font("Consolas", 10);
        txtReporte.BackColor = Color.White;
        txtReporte.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pnlReportes.Controls.Add(txtReporte);
    }

    private void ConstruirCertificados()
    {
        pnlCertificados.Controls.Add(Interfaz.CrearTitulo("Generar certificado escolar", 30, 25));
        pnlCertificados.Controls.Add(Interfaz.CrearEtiqueta("Nombre completo del estudiante:", 33, 95));
        txtAlumnoCertificado = Interfaz.CrearCaja(290, 92, 430);
        pnlCertificados.Controls.Add(txtAlumnoCertificado);

        pnlCertificados.Controls.Add(Interfaz.CrearEtiqueta("DNI del estudiante:", 33, 145));
        txtDniCertificado = Interfaz.CrearCaja(290, 142, 220);
        pnlCertificados.Controls.Add(txtDniCertificado);

        pnlCertificados.Controls.Add(Interfaz.CrearEtiqueta("Grado y sección:", 33, 195));
        txtGradoCertificado = Interfaz.CrearCaja(290, 192, 220);
        pnlCertificados.Controls.Add(txtGradoCertificado);

        pnlCertificados.Controls.Add(Interfaz.CrearEtiqueta("Año académico:", 33, 245));
        txtAnioCertificado = Interfaz.CrearCaja(290, 242, 120);
        txtAnioCertificado.Text = DateTime.Now.Year.ToString();
        pnlCertificados.Controls.Add(txtAnioCertificado);

        pnlCertificados.Controls.Add(Interfaz.CrearEtiqueta("Tipo de reconocimiento:", 33, 295));
        cboTipoCertificado = Interfaz.CrearCombo(290, 292, 280);
        cboTipoCertificado.Items.Add("Tercio superior");
        cboTipoCertificado.Items.Add("Primer puesto");
        cboTipoCertificado.SelectedIndex = 0;
        pnlCertificados.Controls.Add(cboTipoCertificado);

        Button btnGenerar = Interfaz.CrearBoton("Generar e imprimir certificado", 290, 355, 250);
        btnGenerar.Click += BtnGenerarCertificado_Click;
        pnlCertificados.Controls.Add(btnGenerar);

        pnlCertificados.Controls.Add(Interfaz.CrearEtiqueta(
            "Revise los datos antes de imprimir. El certificado se abrirá en vista previa.", 33, 410));
    }

    // ---------------------------------------------------------
    // Navegación
    // ---------------------------------------------------------
    private void Mostrar(Panel seccion)
    {
        pnlInicio.Visible = false;
        pnlRegistro.Visible = false;
        pnlBandeja.Visible = false;
        pnlReportes.Visible = false;
        pnlCertificados.Visible = false;
        seccion.Visible = true;
    }

    private void BtnInicio_Click(object sender, EventArgs e)
    {
        ActualizarInicio();
        Mostrar(pnlInicio);
    }

    private void BtnRegistro_Click(object sender, EventArgs e)
    {
        Mostrar(pnlRegistro);
    }

    private void BtnBandeja_Click(object sender, EventArgs e)
    {
        ActualizarLista();
        Mostrar(pnlBandeja);
    }

    private void BtnReportes_Click(object sender, EventArgs e)
    {
        txtReporte.Text = Tramites.TextoReporte();
        Mostrar(pnlReportes);
    }

    private void BtnCertificados_Click(object sender, EventArgs e)
    {
        if (Usuarios.Actual != 0)
        {
            MessageBox.Show("Esta opción está disponible solo para el director.");
            return;
        }
        Mostrar(pnlCertificados);
    }

    private void BtnGenerarCertificado_Click(object sender, EventArgs e)
    {
        if (Usuarios.Actual != 0)
        {
            MessageBox.Show("Solo el director puede emitir certificados.");
            return;
        }

        string alumno = Validaciones.LimpiarTexto(txtAlumnoCertificado.Text);
        if (!Validaciones.ValidarNombre(alumno))
        {
            MessageBox.Show("Ingrese el nombre del estudiante usando letras y espacios.");
            return;
        }

        string dni = txtDniCertificado.Text.Trim();
        if (!Validaciones.ValidarDNI(dni))
        {
            MessageBox.Show(Validaciones.UltimoError);
            return;
        }

        string grado = Validaciones.LimpiarTexto(txtGradoCertificado.Text);
        if (grado == "")
        {
            MessageBox.Show("Ingrese el grado y sección del estudiante.");
            return;
        }

        int anio;
        if (!int.TryParse(txtAnioCertificado.Text.Trim(), out anio) || anio < 2000 || anio > 2100)
        {
            MessageBox.Show("Ingrese un año académico válido.");
            return;
        }

        string nl = Environment.NewLine;
        string texto = "INSTITUCIÓN EDUCATIVA" + nl +
                       "I.E. Darío Arrús Cuestas N.° 5011" + nl + nl +
                       "CERTIFICADO DE " + cboTipoCertificado.Text.ToUpper() + nl + nl +
                       "El Director de la I.E. Darío Arrús Cuestas N.° 5011 certifica que:" + nl + nl +
                       "\"" + alumno + "\"" + nl +
                       "Identificado(a) con DNI N.° " + dni + ", estudiante de " + grado + "," + nl +
                       "ha obtenido el reconocimiento de " + cboTipoCertificado.Text + nl +
                       "durante el año académico " + anio + "." + nl + nl +
                       "Se expide el presente certificado a solicitud del interesado." + nl + nl +
                       "Callao, " + DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy") + nl + nl + nl +
                       "__________________________________" + nl +
                       Usuarios.Nombres[Usuarios.Actual] + nl +
                       "Director" + nl +
                       "I.E. Darío Arrús Cuestas N.° 5011";

        Impresion.Imprimir("Certificado escolar", texto, 12);
    }

    private void BtnCerrarSesion_Click(object sender, EventArgs e)
    {
        Close();
    }

    // ---------------------------------------------------------
    // Inicio
    // ---------------------------------------------------------
    private void ActualizarInicio()
    {
        int pendientes = Tramites.ContarPendientes();
        int atendidos = Tramites.ContarAtendidos();

        lblTotal.Text = (pendientes + atendidos).ToString();
        lblPendientes.Text = pendientes.ToString();
        lblAtendidos.Text = atendidos.ToString();

        listaRecientes.Items.Clear();
        int mostrados = 0;
        for (int i = 0; i < Datos.Contador && mostrados < 8; i++)
        {
            if (Tramites.PuedeVer(i) && Tramites.EstaPendiente(i))
            {
                listaRecientes.Items.Add(Tramites.Linea(i));
                mostrados++;
            }
        }
        if (mostrados == 0)
            listaRecientes.Items.Add("No hay trámites pendientes.");
    }

    // ---------------------------------------------------------
    // Registrar trámite
    // ---------------------------------------------------------
    private void CboTipo_Changed(object sender, EventArgs e)
    {
        ActualizarCampoAlumno();
    }

    private void ActualizarCampoAlumno()
    {
        bool esJustificacion = cboTipo.SelectedIndex >= 0 &&
                               Datos.TiposValidos[cboTipo.SelectedIndex] == "justificacion";
        lblAlumnoTramite.Visible = esJustificacion;
        txtAlumnoTramite.Visible = esJustificacion;
        if (!esJustificacion)
            txtAlumnoTramite.Clear();
    }

    private void BtnRegistrar_Click(object sender, EventArgs e)
    {
        string solicitante = Validaciones.LimpiarTexto(txtNombre.Text);
        if (!Validaciones.ValidarNombre(solicitante))
        {
            MessageBox.Show(Validaciones.UltimoError);
            return;
        }

        string parentesco = cboParentesco.Text;
        if (parentesco == "Otro")
        {
            parentesco = Validaciones.LimpiarTexto(txtOtro.Text);
            if (parentesco == "")
            {
                MessageBox.Show("Error: especifique el parentesco.");
                return;
            }
        }

        string dni = txtDni.Text.Trim();
        if (!Validaciones.ValidarDNI(dni))
        {
            MessageBox.Show(Validaciones.UltimoError);
            return;
        }

        string telefono = txtTelefono.Text.Trim();
        if (!Validaciones.ValidarTelefono(telefono))
        {
            MessageBox.Show(Validaciones.UltimoError);
            return;
        }

        int posTipo = cboTipo.SelectedIndex;
        string motivo = "-";
        if (Datos.TiposValidos[posTipo] == "justificacion")
        {
            motivo = Validaciones.LimpiarTexto(txtMotivo.Text);
            if (motivo == "")
            {
                MessageBox.Show("Error: ingrese el motivo de la justificación.");
                return;
            }
        }

        string alumno = "-";
        if (Datos.TiposValidos[posTipo] == "justificacion")
        {
            alumno = Validaciones.LimpiarTexto(txtAlumnoTramite.Text);
            if (!Validaciones.ValidarNombre(alumno))
            {
                MessageBox.Show("Ingrese el nombre y apellido del alumno usando letras y espacios.");
                return;
            }
        }

        int posDestino = cboDestinoRegistro.SelectedIndex;
        if (posDestino == -1)
        {
            MessageBox.Show("Error: elija a quién va dirigido el trámite.");
            return;
        }

        string encargado = Validaciones.LimpiarTexto(txtEncargado.Text);
        if (!Validaciones.ValidarNombre(encargado))
        {
            MessageBox.Show(Validaciones.UltimoError);
            return;
        }

        int expediente = Tramites.Registrar(solicitante, parentesco, dni, telefono,
                                            posTipo, motivo, alumno, posDestino, encargado);
        if (expediente == -1)
        {
            MessageBox.Show("Capacidad máxima alcanzada.");
            return;
        }

        string nl = Environment.NewLine;
        DialogResult respuesta = MessageBox.Show(
            "Trámite registrado." + nl +
            "N° de expediente: " + expediente + nl +
            "Dirigido a: " + Usuarios.Etiqueta(posDestino) + nl + nl +
            "¿Desea imprimir el cargo para el padre o apoderado?",
            "Trámite registrado", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

        if (respuesta == DialogResult.Yes)
            Impresion.Imprimir("Cargo de recepción", Tramites.TextoCargo(expediente), 12);

        LimpiarRegistro();
    }

    private void BtnLimpiar_Click(object sender, EventArgs e)
    {
        LimpiarRegistro();
    }

    private void LimpiarRegistro()
    {
        txtNombre.Clear();
        txtOtro.Clear();
        txtDni.Clear();
        txtTelefono.Clear();
        txtMotivo.Clear();
        txtAlumnoTramite.Clear();
        txtEncargado.Clear();
        cboParentesco.SelectedIndex = 0;
        cboTipo.SelectedIndex = 0;
        cboDestinoRegistro.SelectedIndex = -1;
    }

    // ---------------------------------------------------------
    // Bandeja de trámites
    // ---------------------------------------------------------
    // Vuelve a llenar la lista según el filtro elegido.
    private void ActualizarLista()
    {
        lista.Items.Clear();
        totalLista = 0;

        for (int i = 0; i < Datos.Contador; i++)
        {
            if (!Tramites.PuedeVer(i))
                continue;

            bool pendiente = Tramites.EstaPendiente(i);
            if (cboFiltro.SelectedIndex == 1 && !pendiente)
                continue;
            if (cboFiltro.SelectedIndex == 2 && pendiente)
                continue;

            lista.Items.Add(Tramites.Linea(i));
            posLista[totalLista] = i;
            totalLista++;
        }

        if (totalLista == 0)
            lista.Items.Add("No hay trámites para mostrar.");

        lblConteo.Text = "Pendientes: " + Tramites.ContarPendientes() +
                         "   |   Atendidos: " + Tramites.ContarAtendidos();
    }

    // Lee el número de expediente de la caja. Devuelve 0 si no es válido.
    private int LeerExpediente()
    {
        int numero;
        if (!int.TryParse(txtExp.Text, out numero) || numero <= 0)
        {
            MessageBox.Show("Error: ingrese un número de expediente válido.");
            return 0;
        }
        return numero;
    }

    private void CboFiltro_Changed(object sender, EventArgs e)
    {
        ActualizarLista();
    }

    // Al elegir una fila de la lista, se copia su número de expediente a la caja.
    private void Lista_Changed(object sender, EventArgs e)
    {
        int fila = lista.SelectedIndex;
        if (fila >= 0 && fila < totalLista)
            txtExp.Text = Datos.Expedientes[posLista[fila]].ToString();
    }

    private void BtnDetalle_Click(object sender, EventArgs e)
    {
        int numero = LeerExpediente();
        if (numero == 0)
            return;

        MessageBox.Show(Tramites.Buscar(numero), "Expediente " + numero);
    }

    private void BtnAtender_Click(object sender, EventArgs e)
    {
        int numero = LeerExpediente();
        if (numero == 0)
            return;

        MessageBox.Show(Tramites.MarcarAtendido(numero));
        ActualizarLista();
    }

    private void BtnCargo_Click(object sender, EventArgs e)
    {
        int numero = LeerExpediente();
        if (numero == 0)
            return;

        string texto = Tramites.TextoCargo(numero);
        if (texto == "")
        {
            MessageBox.Show("No se encontró el expediente o no está dirigido a usted.");
            return;
        }
        Impresion.Imprimir("Cargo de recepción", texto, 12);
    }

    private void BtnConstancia_Click(object sender, EventArgs e)
    {
        int numero = LeerExpediente();
        if (numero == 0)
            return;

        string texto = Tramites.TextoConstancia(numero);
        if (texto == "")
        {
            MessageBox.Show("No se puede emitir la constancia: el expediente no existe, " +
                            "no está dirigido a usted o todavía no fue atendido.");
            return;
        }
        Impresion.Imprimir("Constancia de atención", texto, 12);
    }

    private void BtnDerivar_Click(object sender, EventArgs e)
    {
        int numero = LeerExpediente();
        if (numero == 0)
            return;

        if (cboDestino.SelectedIndex == -1)
        {
            MessageBox.Show("Error: elija a quién desea derivar el trámite.");
            return;
        }

        MessageBox.Show(Tramites.Derivar(numero, cboDestino.SelectedIndex));
        ActualizarLista();
    }

    private void BtnOrdenar_Click(object sender, EventArgs e)
    {
        Tramites.Ordenar();
        ActualizarLista();
        MessageBox.Show("Trámites ordenados por expediente (más recientes primero).");
    }

    // ---------------------------------------------------------
    // Reportes
    // ---------------------------------------------------------
    private void BtnGenerar_Click(object sender, EventArgs e)
    {
        txtReporte.Text = Tramites.TextoReporte();
    }

    private void BtnImprimirReporte_Click(object sender, EventArgs e)
    {
        Impresion.Imprimir("Reporte de trámites", Tramites.TextoReporte(), 8.5f);
    }

    private void BtnGuardarReporte_Click(object sender, EventArgs e)
    {
        string ruta = Archivos.GuardarReporte(Tramites.TextoReporte());
        if (ruta != "")
            MessageBox.Show("Reporte guardado en:" + Environment.NewLine + ruta);
    }
}
