using System;

internal static class Punto6Pruebas
{
    private static int aprobadas;

    private static void Verificar(bool condicion, string nombre)
    {
        if (!condicion)
            throw new Exception("FALLÓ: " + nombre);

        aprobadas++;
        Console.WriteLine("OK: " + nombre);
    }

    private static int Registrar(string nombre, string dni, string telefono, int destino)
    {
        return Tramites.Registrar(nombre, "Madre", dni, telefono, 1, "-", "-",
                                  destino, "Mesa de Partes");
    }

    public static int Main()
    {
        Verificar(Validaciones.ValidarNombre("Ana Ejemplo"), "nombre válido");
        Verificar(!Validaciones.ValidarNombre("Ana123"), "rechazo de nombre con números");
        Verificar(Validaciones.ValidarDNI("00000001"), "DNI de ocho dígitos");
        Verificar(!Validaciones.ValidarDNI("123"), "rechazo de DNI incompleto");
        Verificar(Validaciones.ValidarTelefono("900000001"), "teléfono celular válido");
        Verificar(!Validaciones.ValidarTelefono("800000001"), "rechazo de teléfono que no empieza en 9");

        Datos.Contador = 0;
        Usuarios.Actual = 7;
        Verificar(Registrar("Ana Ejemplo", "00000001", "900000001", 0) == 1, "registro de expediente");
        Verificar(Registrar("Luis Prueba", "00000002", "900000002", 1) == 2, "numeración correlativa");
        Verificar(Registrar("Rosa Muestra", "00000003", "900000003", 2) == 3, "tercer registro");

        Usuarios.Actual = 0;
        Verificar(Tramites.Buscar(1).Contains("Ana Ejemplo"), "búsqueda de expediente existente");
        Verificar(Tramites.Buscar(999).Contains("no encontrado"), "búsqueda de expediente inexistente");

        string consulta = Tramites.Consultar(1, "00000001");
        Verificar(consulta.Contains("Estado: Recibido"), "consulta pública con número y DNI correctos");
        Verificar(!consulta.Contains("900000001") && !consulta.Contains("00000001"),
                  "consulta pública no expone teléfono ni DNI");
        Verificar(Tramites.Consultar(1, "00000000").Contains("No se encontró"),
                  "rechazo de consulta con DNI incorrecto");

        Tramites.Ordenar();
        Verificar(Datos.Expedientes[0] == 3 && Datos.Expedientes[1] == 2 && Datos.Expedientes[2] == 1,
                  "orden descendente de expedientes");
        Verificar(Datos.Solicitantes[2] == "Ana Ejemplo" && Datos.Dnis[2] == "00000001",
                  "ordenamiento conserva asociación entre campos");

        Verificar(Tramites.Derivar(1, 1).Contains("Coordinador"), "derivación de expediente");
        Verificar(Datos.Estados[Tramites.BuscarPosicion(1)] == "Derivado", "actualización del estado al derivar");
        Usuarios.Actual = 1;
        Verificar(Tramites.MarcarAtendido(1).Contains("marcado como atendido"), "marcar expediente atendido");

        Archivos.Cargar();
        int posicion = Tramites.BuscarPosicion(1);
        Verificar(Datos.Contador == 3 && posicion >= 0, "persistencia y recarga de expedientes");
        Verificar(Datos.Estados[posicion] == "Atendido" && Datos.Dnis[posicion] == "00000001",
                  "persistencia de estado y datos relacionados");

        Console.WriteLine("RESULTADO: " + aprobadas + " comprobaciones aprobadas.");
        return 0;
    }
}
