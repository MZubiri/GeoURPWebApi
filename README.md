# GeoURP - Web API Backend 📚

API REST central y motor de persistencia para la **Biblioteca Digital de Geotecnia (GeoURP)**, desarrollada para dar soporte a las asociaciones estudiantiles de la **Universidad Ricardo Palma (URP)**.

Este sistema centraliza el catálogo de documentos técnicos, tesis, normativas y libros del área de Geotecnia, gestionando la autenticación de alumnos, préstamos digitales, descargas y notificaciones automáticas.

---

## 🚀 Características Principales

*   **Autenticación & Autorización:** Implementa un flujo seguro basado en **Tokens JWT** con firma digital.
*   **Gestión de Catálogo Digital:** Endpoints estructurados para la carga, actualización y descarga segura de documentos técnicos y académicos.
*   **Integración SMTP (Brevo):** Sistema de notificaciones por correo electrónico integrado con **Brevo** para el registro de nuevos usuarios, alertas de préstamo y recuperación de contraseñas.
*   **Diseño Limpio:** Arquitectura estructurada con Controladores, DTOs para transferencia de datos limpia, Servicios de lógica de negocio y capa de datos.
*   **Persistencia Robusta:** Conexión relacional gestionada con **SQL Server** y EF Core.

---

## 🛠️ Stack Tecnológico

*   **Lenguaje & Framework:** C# / .NET 8 / ASP.NET Core
*   **Base de Datos:** SQL Server (alojado en entorno de producción)
*   **Notificaciones:** Brevo SMTP (Envío de correos)
*   **Seguridad:** Spring/JWT (.NET Authentication)
*   **Servidor de Producción:** VPS Linux (Contabo) corriendo Ubuntu Server.

---

## 🔗 Repositorio del Frontend

La interfaz de usuario web que interactúa con esta API se encuentra en:
👉 **[geo-urp (Frontend Angular)](https://github.com/MZubiri/geo-urp)**

---

## 📦 Ejecución Local

### Prerrequisitos
*   .NET 8.0 SDK instalado.
*   Servidor SQL Server activo localmente.

### Pasos
1.  Clona el repositorio:
    ```bash
    git clone https://github.com/MZubiri/GeoURPWebApi.git
    cd GeoURPWebApi
    ```
2.  Configura tu cadena de conexión en `GeoURPWebApi/appsettings.json` o mediante variables de entorno.
3.  Restaura dependencias y ejecuta el servidor:
    ```bash
    dotnet run --project GeoURPWebApi/GeoURPWebApi
    ```

La API se levantará en los puertos locales predeterminados. Puedes verificar la documentación interactiva de endpoints mediante el archivo de especificación `swagger.json`.
