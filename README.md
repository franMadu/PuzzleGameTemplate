# OneHandPuzzle

Base en Unity para una serie de juegos narrativos con conversaciones y puzzles deslizantes. La historia puede encadenar varias conversaciones antes de cada puzzle. El contenido alcanzado se puede repetir desde una biblioteca de capítulos y las imágenes completadas se conservan en una galería.

Estado documentado: 23 de noviembre de 2026. Contenido actual: **capítulo 1 de Shouta y Ayame**.

## Documentación

El [documento editable de diálogos](Docs/DIALOGOS_DEL_JUEGO.docx) reúne las conversaciones del capítulo 1, con personajes, pensamientos y números de intervención. Se puede corregir en Word o LibreOffice; los cambios del documento se trasladan después a Unity y no se aplican automáticamente al juego.

| Documento | Contenido |
| --- | --- |
| [Guía de contenido](Docs/GUIA_DE_CONTENIDO.md) | Configurar un juego, capítulos, subcapítulos, conversaciones, personajes, imágenes y música. Incluye un capítulo nuevo paso a paso. |
| [Referencia técnica](Docs/REFERENCIA_TECNICA.md) | Escenas, scripts, flujo, cálculo del mínimo, notas, guardado, transiciones y modo dios. |
| [Validación y limitaciones](Docs/VALIDACION_Y_LIMITACIONES.md) | Pruebas realizadas, comprobación manual, diagnóstico de problemas y límites actuales. |

## Abrir y ejecutar

1. Abrir el proyecto con **Unity 6000.3.16f1**. Las dependencias están declaradas en [Packages/manifest.json](Packages/manifest.json); se utilizan uGUI, Input System y Universal Render Pipeline.
2. Abrir [MainMenu.unity](Assets/Scenes/MainMenu.unity).
3. Entrar en Play, pulsar **Empezar juego** y elegir **Modo historia**.

El menú principal es la primera escena habilitada para la compilación. También están habilitadas `Story` y las tres escenas de dificultad.

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
| Cuadro de texto, Espacio o Intro | Completar el texto que se está escribiendo; después avanzar de página. La flecha indica que el texto ha terminado. |
| Ocultar diálogo | Ocultar el cuadro; pulsar sobre la pantalla lo muestra de nuevo. |
| Menú principal, esquina superior izquierda | Volver al menú sin completar la conversación. Al continuar, esa conversación comienza desde el principio. |
| X inferior izquierda o Escape | Omitir el resto de la conversación actual y continuar el flujo. |

El reparto puede contener más de dos personajes, aunque solo hay dos visibles a la vez. Los cambios de personaje tienen salida y entrada lateral, y las imágenes se reflejan según su posición y orientación original.

Cada personaje puede tener varios `Outfits`, con sprite y orientación propios. Las conversaciones eligen el vestuario inicial y pueden cambiarlo por intervención. El outfit se conserva dentro de esa conversación, también al salir y volver a entrar, sin duplicar el personaje ni alterar su retrato predeterminado. La [guía de contenido](Docs/GUIA_DE_CONTENIDO.md#varios-outfits-para-un-personaje) explica cómo configurarlo en el Inspector.

Las expresiones son independientes del outfit. `Expression` cambia la de quien habla y `Reactions` permite cambiar las de otros personajes en la misma intervención. Se conservan hasta modificarlas o restablecerlas; cada vestuario tiene sus sprites de expresión. Véase [expresiones y reacciones](Docs/GUIA_DE_CONTENIDO.md#expresiones-y-reacciones).

### Galería

La galería tiene dos categorías: **Puzles** y **Fondos de pantalla**. Cada combinación de puzzle y dificultad completada tiene una entrada independiente, siempre que el puzzle se haya alcanzado en la historia. Los fondos muestran la imagen inicial y las recompensas ganadas al terminar subcapítulos. Un subcapítulo puede conceder varios fondos; los desbloqueos se conservan al repetir la historia y se recuperan para las partidas anteriores.

Las miniaturas muestran solo la imagen con un marco fino. Pulsar una miniatura abre la imagen completa ajustada a la pantalla, sin deformarla. Los botones **+** y **−** y la rueda del ratón permiten ampliar hasta 4×; al ampliar se puede arrastrar la imagen. **Volver** regresa a la galería. Solo los fondos desbloqueados ofrecen **Usar como fondo**, que guarda la elección para las siguientes sesiones.

### Configuración y salida

El engranaje abre los controles de volumen general, música, efectos, silencio, velocidad del texto y opacidad del diálogo. En plataformas compatibles también permite elegir resolución y ventana o pantalla completa. **Restablecer ajustes** conserva la historia, las notas y las imágenes desbloqueadas.

En Unity Editor aparece además **Modo dios**, que señala la pieza del siguiente movimiento óptimo. Esta opción y el solucionador de pistas están excluidos de las compilaciones del juego, incluidas las de desarrollo.


## Contenido actual

El capítulo 1 contiene ocho conversaciones escritas: `1-1`, `1-2`, `1-3`, `1-4`, `1-4 Cierre`, `1-5`, `1-6` y `1-6 cierre`. Los personajes configurados son Shouta, Ayame y el padre de Shouta.

`Story.asset` enlaza actualmente las conversaciones `1-1`, `1-2` y `1-3`, con el puzle `1-2` entre las dos últimas. Las cinco conversaciones restantes están preparadas como assets y todavía no forman parte del recorrido. El catálogo conserva un único puzle, con imágenes propias para las tres dificultades.

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
  Scenes/              Menú, historia y dificultades
  Scripts/             Sistemas del juego
Docs/                  Guías y referencia
```

Para añadir contenido, comenzar por la [guía de configuración](Docs/GUIA_DE_CONTENIDO.md). Los límites del guardado, de los recursos y de la validación se detallan en [Validación y limitaciones](Docs/VALIDACION_Y_LIMITACIONES.md).
