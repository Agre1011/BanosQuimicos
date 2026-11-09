# Ejecución local: Windows y Android

Requisitos: Visual Studio 2026, SDK .NET 10, workloads MAUI/Android y SQL Server LocalDB. No instalar/actualizar componentes ni reinicializar bases como comprobación rutinaria. Equipo nuevo: revisar [base de datos](base-datos.md).

## API y primer administrador

1. Abrir `BanosQuimicos.sln`, esperar restauración y elegir Debug. Si LocalDB no responde por nombre, consultar [diagnóstico](localdb.md).
2. Desde PowerShell en la raíz, ejecutar `./scripts/configurar-jwt-local.ps1`: conserva la clave o genera una en User Secrets, fuera de Git. No mostrar/exportar el valor.
3. Solo si no existe ningún administrador y después de comprobar la base destino, detener la API y ejecutar `dotnet run --project BanosQuimicos.Api --launch-profile http -- --crear-admin-local`. Pide datos y contraseña nueva con confirmación, sin mostrarla ni recibirla por argumentos. Solo Development; no crea otro si existe cualquier administrador ni convierte un correo registrado. Crea Administrador/Activa sin perfil ficticio y termina sin iniciar servidor.
4. API como inicio, perfil **https**, F5; alternativa: `dotnet run --project BanosQuimicos.Api --launch-profile https`. Mantenerla abierta al usar la app.

El perfil abre `https://localhost:7292` y `http://localhost:5148`. GET `/api/estado`: 200 y mensaje de funcionamiento. No identifica base ni prueba por sí solo SQL. `/` puede responder 404; no hay Swagger configurado.

Windows requiere certificado local confiable: `dotnet dev-certs https --check`; si hace falta, establecer confianza personalmente con `dotnet dev-certs https --trust`. No desactivar validación.

## Elegir servidor

`BanosQuimicos.App/Plataforma/ConexionApi.cs`: `UsarHosting = false` selecciona local (actual); `true`, `https://celeris.runasp.net/` en ambos destinos, incluso Debug. Recompilar al cambiarlo.

- Windows local: HTTPS 7292.
- Android Debug local: HTTP `127.0.0.1:5148` por USB. Excepción limitada a ese dominio y Debug; no desactiva validación HTTPS.
- Hosting: HTTPS válido, sin túnel ni API local; no usarlo para funciones aún no publicadas.

Verificar destino por selector/configuración compilada y peticiones/log de esa API. El mensaje `/api/estado` es igual en ambos. Cambiar servidor elimina sesión anterior; prueba manual de alternancia en dispositivo pendiente. No copiar cuentas/bases locales al hosting.

## Windows

Con API abierta, App como inicio, **Windows Machine**, Debug, F5. Se admite inicio múltiple con API HTTPS. Si una compilación informa DLL bloqueada, detener ordenadamente la API anterior; no borrar datos ni terminar procesos ajenos.

## Android físico por USB

1. Moto G9 Plus con depuración USB autorizada; API escuchando HTTP 5148. Sin abrir firewall ni publicar la PC en red.
2. Usar ADB instalado (ajustar ruta si el SDK está en otra carpeta):

```powershell
$adbBanos = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
& $adbBanos devices -l
& $adbBanos reverse tcp:5148 tcp:5148
& $adbBanos reverse --list
```

Se espera `device` y mapeo 5148. Con varios teléfonos, `-s SERIAL` antes de `reverse`. Repetir túnel tras reconectar/reiniciar. `unauthorized` requiere autorización en el teléfono.

3. Elegir Android, Debug y teléfono en Visual Studio; F5. No distribuir Release con loopback local; [publicación](publicacion.md) exige destino HTTPS alojado.

## Compilar

Desde raíz: `dotnet build BanosQuimicos.App -f net10.0-windows10.0.19041.0 -c Debug` y `dotnet build BanosQuimicos.App -f net10.0-android -c Debug`. Compilar no prueba teclado, navegación, SecureStorage nativo ni apariencia. Evidencia real: [verificaciones](verificaciones.md).
