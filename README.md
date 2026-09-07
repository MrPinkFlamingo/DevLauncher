# DevLauncher

Launcher moderno de Minecraft para Windows (Java Edition).

## Características

- Soporte para todas las versiones de Minecraft Java (No-Premium / Offline).
- Descarga y actualización automática de versiones directamente desde los servidores oficiales de Mojang.
- Compatibilidad con Forge, Fabric, Quilt y OptiFine.
- Control de memoria RAM asignada al juego con deslizador.
- Gestor integrado de skins (carga local y descarga por nickname).
- Ventanas modales y diseño oscuro fluido.
- Pestaña con consola y registros del juego.

## Estructura del Proyecto

- `src/`: Código fuente del launcher.
- `installer/`: Código fuente del instalador autónomo.
- `Compilar_Launcher.bat`: Script de compilación del launcher.
- `Compilar_Instalador.bat`: Script de compilación del instalador.

## Compilación

Requisitos: .NET 8 SDK instalado.

Para compilar el launcher:
```bat
Compilar_Launcher.bat
```

Para generar el instalador:
```bat
Compilar_Instalador.bat
```
