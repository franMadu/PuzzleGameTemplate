# OneHandPuzzle

Base en Unity para una serie de juegos narrativos con conversaciones y puzzles deslizantes. La historia puede encadenar varias conversaciones antes de cada puzzle. El contenido alcanzado se puede repetir desde una biblioteca de capítulos y las imágenes completadas se conservan en una galería.

Estado documentado: 19 de noviembre de 2026. Juego de ejemplo: **El camino de los recuerdos**.

## Documentación

| Documento | Contenido |
| --- | --- |
| [Guía de contenido](Docs/GUIA_DE_CONTENIDO.md) | Configurar un juego, capítulos, subcapítulos, conversaciones, personajes, imágenes y música. Incluye un capítulo nuevo paso a paso. |
| [Referencia técnica](Docs/REFERENCIA_TECNICA.md) | Escenas, scripts, flujo, cálculo del mínimo, notas, guardado, transiciones y modo dios. |
| [Validación y limitaciones](Docs/VALIDACION_Y_LIMITACIONES.md) | Pruebas realizadas, comprobación manual, diagnóstico de problemas y límites actuales. |

## Abrir y ejecutar

1. Abrir el proyecto con **Unity 6000.3.16f1**. Las dependencias están declaradas en [Packages/manifest.json](Packages/manifest.json); se utilizan uGUI, Input System y Universal Render Pipeline.
2. Abrir [MainMenu.unity](Assets/Scenes/MainMenu.unity).
3. Entrar en Play, pulsar **Empezar juego** y elegir **Modo historia**.
4. Para probar solo las conversaciones, abrir [DialogueDemo.unity](Assets/Scenes/DialogueDemo.unity).

El menú principal es la primera escena habilitada para la compilación. También están habilitadas `Story`, las tres escenas de dificultad y `DialogueDemo`. La escena de demostración no forma parte del recorrido normal del juego.

## Funcionamiento para el jugador

Al abrir el juego aparece una pantalla de título con el nombre del catálogo y **Empezar juego**. Este botón abre el menú principal, con una imagen de fondo configurable, el engranaje de ajustes arriba a la derecha y tres botones en la parte inferior: **Galería**, **Modo historia** y **Recapitulación**. Recapitulación abre los capítulos y subcapítulos desbloqueados. Al volver desde otras pantallas se regresa al menú principal sin repetir la pantalla de título.

### Historia

La partida recorre los pasos de `Story.asset` en orden. Un paso puede reproducir una conversación o abrir un puzzle. La dificultad de cada puzzle la establece el autor en el catálogo; el jugador no la elige durante la historia.

Al completar un puzzle se guarda el resultado y comienza un destello blanco gradual. Durante el destello se sustituye el tablero por la imagen completa, incluida la casilla inferior derecha. Al terminar aparece **Continuar**. Los resultados se muestran cuando el jugador pulsa ese botón; después puede continuar la historia.

Se guarda el siguiente paso pendiente. Salir de una conversación sin terminarla o abandonar un puzzle sin resolverlo no conserva la línea ni la distribución del tablero.

### Capítulos y subcapítulos

La selección tiene dos niveles. Solo aparecen los capítulos con contenido desbloqueado y los subcapítulos que ya se han alcanzado en la historia.

- Un subcapítulo de conversación la reproduce y vuelve a la selección de su capítulo.
- Un subcapítulo con puzzle abre un submenú con pestañas **Fácil**, **Normal** y **Difícil**, la imagen y la mejor nota de esa dificultad. Al jugar se reproducen las conversaciones incluidas antes del puzzle y las de cierre ya alcanzadas.
- Una dificultad sin completar muestra `?` y **Sin completar**. Completar otra dificultad no revela su imagen.

Las repeticiones actualizan las notas y las imágenes completadas, pero no avanzan el punto guardado de la historia ni desbloquean capítulos futuros. Repetir la historia desde el principio conserva los desbloqueos anteriores.

### Puzzle

| Dificultad | Tablero | Piezas |
| --- | --- | --- |
| Fácil | 3×3 | 8 |
| Normal | 4×4 | 15 |
| Difícil | 5×5 | 24 |

Cada dificultad tiene una imagen configurable de forma independiente. El objetivo es ordenar las piezas deslizando una pieza vecina hacia el hueco. Solo se permiten movimientos horizontales o verticales. Se puede pulsar una pieza válida o arrastrarla hacia el hueco; el arrastre se confirma a partir del 25 % de la distancia entre casillas.

El tablero muestra los movimientos realizados y el mínimo de su distribución inicial. **Mezclar** genera una distribución nueva y reinicia el contador. La nota va de **S** a **D** según cuánto se supere ese mínimo.

### Conversaciones

El texto aparece en un cuadro semitransparente casi pegado al borde inferior. El nombre ocupa una pestaña sobre el cuadro. Los personajes aparecen grandes en los laterales, detrás del cuadro de texto; quien escucha se oscurece y se reduce ligeramente.

| Control | Acción |
| --- | --- |
| Flecha inferior derecha, Espacio o Intro | Completar el texto que se está escribiendo; después avanzar de página. |
| X inferior izquierda o Escape | Omitir el resto de la conversación actual y continuar el flujo. |

El reparto puede contener más de dos personajes, aunque solo hay dos visibles a la vez. Los cambios de personaje tienen salida y entrada lateral, y las imágenes se reflejan según su posición y orientación original.

### Galería

Cada combinación de puzzle y dificultad completada tiene una entrada independiente, siempre que el puzzle se haya alcanzado en la historia. Las miniaturas muestran solo la imagen con un marco fino. Pulsar una miniatura abre la imagen completa ajustada a la pantalla, sin deformarla. Los botones **+** y **−** y la rueda del ratón permiten ampliar hasta 4×; al ampliar se puede arrastrar la imagen. **Volver** regresa a la galería. **Usar como fondo** guarda esa imagen como fondo del menú principal, también para las siguientes sesiones.

### Configuración y salida

El engranaje abre los controles de volumen general, música, efectos, silencio, velocidad del texto y opacidad del diálogo. En plataformas compatibles también permite elegir resolución y ventana o pantalla completa. **Restablecer ajustes** conserva la historia, las notas y las imágenes desbloqueadas.

En Unity Editor aparece además **Modo dios**, que señala la pieza del siguiente movimiento óptimo. Esta opción y el solucionador de pistas están excluidos de las compilaciones del juego, incluidas las de desarrollo.


## Contenido de ejemplo

| Capítulo | Subcapítulos | Dificultad del puzzle en historia |
| --- | --- | --- |
| El valle | Llegada al valle; El paso del valle | Fácil |
| La costa | Llegada a la costa; Al pie del faro; Dentro del faro | Normal |
| El desierto | Entre las dunas; El campamento; El mirador de las dunas | Difícil |

El ejemplo contiene 14 pasos: 11 conversaciones y 3 puzzles. Los personajes son Viajera, Guía y Mensajera. Las tres imágenes disponibles se reutilizan como variantes de dificultad; todavía no hay nueve ilustraciones originales diferentes. La música de las conversaciones y puzzles está sin asignar, por lo que esos tramos permanecen sin música de fondo.

## Estructura del proyecto

```text
Assets/
  Art/                 Imágenes de puzzles y personajes
  Data/
    PuzzleCatalog.asset
    Story.asset
    Characters/        Personajes reutilizables
    Dialogue/          Conversaciones
  Resources/Audio/     Sonidos de botones y piezas
  Scenes/              Menú, historia, dificultades y demostración
  Scripts/             Sistemas del juego
Docs/                  Guías y referencia
```

Para añadir contenido, comenzar por la [guía de configuración](Docs/GUIA_DE_CONTENIDO.md). Los límites del guardado, de los recursos y de la validación se detallan en [Validación y limitaciones](Docs/VALIDACION_Y_LIMITACIONES.md).
