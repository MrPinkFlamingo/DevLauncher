# DevLauncher

Lanzador moderno, rápido y ligero de **Minecraft Java Edition** para Windows.

<p align="center">
  <a href="https://github.com/MrPinkFlamingo/DevLauncher/releases/latest/download/DevLauncherInstaller.exe">
    <img src="https://img.shields.io/badge/DESCARGAR%20INSTALADOR-DevLauncherInstaller.exe-22c55e?style=for-the-badge&logo=windows&logoColor=white" height="46" alt="Descargar DevLauncher" />
  </a>
</p>

<p align="center">
  <a href="https://github.com/MrPinkFlamingo/DevLauncher/releases/latest/download/DevLauncherInstaller.exe"><b>📥 Descarga Directa (.exe)</b></a> &nbsp;•&nbsp; 
  <a href="https://github.com/MrPinkFlamingo/DevLauncher/releases/latest">📋 Notas de la Versión</a>
</p>

---

## Características

- **Todas las versiones oficiales:** Descarga y actualización automática directamente desde los servidores oficiales de Mojang.
- **Modo No-Premium / Offline:** Juega con cualquier apodo sin necesidad de cuenta de Microsoft.
- **Soporte de Mods:** Compatible con instaladores de Forge, Fabric, Quilt y OptiFine.
- **Control de RAM:** Asigna fácilmente los GB de memoria que prefieras con un deslizador.
- **Gestor de Skins:** Aplica tus skins personalizadas o descarga la de cualquier jugador por su apodo con 1 clic.
- **Diseño fluido y oscuro:** Interfaz moderna con ventanas modales temáticas.
- **Pestaña de Registros:** Consola integrada para monitorear el juego.

---

## Instalación

1. Haz clic en el botón verde de **[DESCARGAR INSTALADOR](https://github.com/MrPinkFlamingo/DevLauncher/releases/latest/download/DevLauncherInstaller.exe)**.
2. Ejecuta el archivo `DevLauncherInstaller.exe`.
3. Haz clic en **Instalar** y ¡listo para jugar!

---

## Estructura del Proyecto

- `src/`: Código fuente del launcher.
- `installer/`: Código fuente del instalador autónomo.
- `Compilar_Launcher.bat`: Script de compilación del launcher.
- `Compilar_Instalador.bat`: Script de compilación del instalador.

## Compilación Manual

Requisitos: .NET 8 SDK instalado.

Para compilar el launcher:
```bat
Compilar_Launcher.bat
```

Para generar el instalador:
```bat
Compilar_Instalador.bat
```
