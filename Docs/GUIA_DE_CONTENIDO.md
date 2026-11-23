# Configuración del contenido de OneHandPuzzle

El juego se configura mediante assets de Unity. Las escenas y los scripts proporcionan el funcionamiento común; el catálogo define los puzzles y la secuencia de historia establece cuándo se reproducen las conversaciones y cuándo se juega.

[Inicio](../README.md) · [Referencia técnica](REFERENCIA_TECNICA.md) · [Validación y limitaciones](VALIDACION_Y_LIMITACIONES.md)

## Diferenciar catálogo e historia

Hay dos campos llamados `Chapters` con funciones distintas:

| Ubicación | Función |
| --- | --- |
| `PuzzleCatalog → Chapters` | Lista de definiciones de **puzzles**. Cada entrada tiene imágenes por dificultad, música y dificultad de historia. El nombre del campo procede de la estructura inicial del proyecto. |
| `Story → Chapters` | **Capítulos narrativos** de la selección y sus subcapítulos. Cada subcapítulo identifica un rango de pasos de la historia. |
| `Story → Steps` | Secuencia lineal real del juego. Sus pasos son conversaciones o puzzles. |

Un capítulo narrativo puede contener solo conversaciones y no necesita una entrada nueva en el catálogo. También puede contener varios puzzles, repartidos entre subcapítulos. Cada subcapítulo admite como máximo un puzzle, más las conversaciones anteriores y posteriores que se deseen incluir.

## Configurar un juego de la serie

1. Crear o duplicar un catálogo. El menú de creación es `Create → One Hand Puzzle → Catalog`.
2. Darle un `Progress Id` permanente y propio, por ejemplo `OneHandPuzzle.NuevoViaje`. Ese valor separa el progreso de otros catálogos.
3. Configurar `Game Title`, `Fondo inicial`, `Ending Text` y la referencia `Story`. `Game Title` aparece en la pantalla inicial; `Fondo inicial` es la textura del menú principal, ajustada para cubrir la pantalla sin deformarse. Esta imagen está disponible desde el principio.
4. Crear o duplicar la secuencia con `Create → One Hand Puzzle → Story Sequence`.
5. Preparar los personajes, conversaciones, definiciones de puzzles y agrupaciones narrativas descritos a continuación.
6. Asignar el mismo catálogo al `PuzzleMenu` de `MainMenu` y a los `PuzzleSceneController` de `PuzzleEasy`, `PuzzleNormal` y `PuzzleHard`, además del `StorySceneController` de `Story`.
7. Para una aplicación independiente, configurar también Company Name y Product Name en Player Settings. Unity los utiliza para separar los `PlayerPrefs` entre aplicaciones.

Para añadir capítulos no hay que crear más escenas. Se reutilizan `Story` y las tres escenas de tablero. Los nombres de estas escenas están definidos en `PuzzleSession`; si se renombran, también hay que actualizar ese código y la lista de escenas de compilación.

## Crear personajes

Crear un asset con `Create → One Hand Puzzle → Dialogue Character` y completar:

| Campo | Uso |
| --- | --- |
| `Display Name` | Nombre que aparece en la pestaña del diálogo. Si está vacío se usa el nombre del asset. |
| `Portrait` | Sprite predeterminado del personaje. Utilizar una imagen con transparencia y encuadre apropiado para mostrarlo aproximadamente de rodillas hacia arriba. |
| `Facing` | Dirección hacia la que mira el dibujo original: `Right` o `Left`. No es el lado de pantalla donde debe aparecer. |
| `Outfits` | Variantes del mismo personaje. Cada entrada tiene un `Id` estable, un `Portrait` y su propio `Facing`. |
| `Expressions` | Expresiones del vestuario predeterminado, con `Id` y `Portrait`. Cada entrada de `Outfits` tiene también su propia lista `Expressions`. |

Importar los retratos como sprites y conservar su transparencia. El sistema refleja horizontalmente el dibujo para orientar a los personajes según el lado que ocupan. No hace falta duplicar el recurso para tener una versión izquierda y otra derecha.

Los personajes del juego están en `Assets/Data/Characters`: `Shouta`, `Ayame` y `Shouta_Father`.

### Varios outfits para un personaje

En el asset del personaje, añadir una entrada en `Outfits` por vestuario, por ejemplo `viaje`, `gala` o `invierno`. Asignar su sprite y la dirección original de ese dibujo. Los identificadores deben ser únicos dentro del personaje; `default` está reservado para el retrato predeterminado. No hace falta duplicar el personaje ni cambiar su nombre para representar otro vestuario.

En cada conversación, `Initial Outfits` permite elegir el vestuario inicial de los personajes del reparto: añadir una entrada, asignar `Character` y seleccionar `Outfit` en el desplegable. Los personajes sin entrada utilizan `Portrait` y `Facing` del asset. El outfit inicial se aplica también a los personajes que están escuchando o entran más tarde.

Cada intervención tiene otro desplegable `Outfit` después de asignar `Character`. **Conservar outfit actual** mantiene la elección anterior; seleccionar un outfit la cambia desde esa intervención; **Predeterminado (restablecer)** vuelve al retrato base. La elección se conserva durante el resto de la conversación, incluso al salir y volver a entrar o al dividir una intervención en varias páginas. Cambiar el vestuario del mismo personaje mantiene su posición, sin animarlo como si llegara otro personaje.

Cada conversación empieza con sus propios outfits iniciales. No se modifica el asset del personaje ni se guarda un vestuario global entre conversaciones: configurar `Initial Outfits` en cada conversación permite que las repeticiones de capítulos muestren el vestuario correcto. Los diálogos existentes no necesitan cambios.

El Inspector señala identificadores duplicados, sprites sin asignar y selecciones que ya no existen. Si se renombra un `Id`, actualizar las conversaciones que lo usan; un identificador inválido emite un aviso al reproducir y utiliza el retrato predeterminado. `Tools > One Hand Puzzle > Validate Dialogue Outfits` comprueba los cambios de outfit, las orientaciones, las salidas y entradas, la paginación y la compatibilidad con las conversaciones existentes.

## Organización de las ventanas

El menú sigue una estructura común de inicio, navegación y detalle. **Inicio** deja visible la ilustración elegida y destaca **Continuar historia**, con el siguiente paso narrativo. La barra inferior conserva cinco destinos en las ventanas del menú: **Inicio**, **Capítulos**, **Galería**, **Desafíos** y **Tienda**. El saldo de estrellas y las opciones ocupan la cabecera; **Volver** mantiene la jerarquía de cada sección.

**Capítulos** funciona como archivo de la historia: capítulos con progreso, subcapítulos con estado disponible/completado/bloqueado, vista previa y estrellas cuando hay puzle. Las entradas bloqueadas se ven pero no pueden abrirse. **Desafíos** permite jugar y repetir los puzles disponibles, elegir dificultad y consultar el récord; un desafío de pago sin comprar remite a la tienda. La **Tienda** agrupa categorías a la izquierda y artículos en una cuadrícula de seis tarjetas, con imagen, precio y acción. La **Galería** conserva tarjetas de solo imagen, separa puzles y fondos y abre el visor con zoom. Solo los fondos pueden seleccionarse para el inicio.

La disposición se inspira en las capturas y guías consultadas: el inicio despejado y los accesos de campaña de [NIKKE](https://cow-snake.github.io/nikke-mg/features/main-screen/), la navegación estable y los mapas narrativos de [Fire Emblem Heroes](https://new-guide.fire-emblem-heroes.com/en-US/feh-1010.html), y las categorías laterales, saldo superior y cuadrícula de la [tienda de Genshin Impact](https://interfaceingame.com/screenshots/genshin-impact-mobile-paimons-bargains/). El [archivo de NIKKE](https://bsky.app/profile/nikke-en.bsky.social/post/3lbmyi2ywps2f) también distingue el acceso a recuerdos de la campaña. Son referencias de organización; los textos, los iconos y el estilo pertenecen a esta plantilla y se adaptan a sus funciones.

Los objetos estáticos, los iconos y la colocación están en el Canvas de `MainMenu`, con anclajes proporcionales para distintas relaciones de aspecto. `Tools > One Hand Puzzle > Organize Menus` aplica esta disposición en el editor. Una escena abierta durante la actualización se migra en el editor conservando los datos del juego. No se crean controles del menú durante la partida.

## Economía de estrellas y tienda

Las estrellas de los puzles son también moneda. Cada puzle y dificultad tiene su propio récord y puede aportar como máximo tres estrellas al saldo durante toda la partida: `0 → 1` suma una, `1 → 1` suma cero, `1 → 2` suma una y `1 → 3` suma dos. Los umbrales de rendimiento siguen siendo los de la puntuación del puzle. Gastar estrellas no borra récords ni permite volver a cobrarlos. La pantalla de resultado muestra lo ganado en ese intento y el saldo disponible; el menú principal y la tienda muestran el saldo junto a un icono de estrella.

El saldo, los récords ya remunerados y las compras se conservan por `Progress Id`. Las partidas anteriores reciben una sola vez las estrellas de sus mejores resultados, incluidas las antiguas notas S–D convertidas a estrellas. Reiniciar o repetir la historia conserva las compras y el saldo. No reutilizar el `Progress Id` para otro juego, ni reordenar las entradas de `Chapters`: los récords anteriores identifican los puzles por su índice y dificultad.

Para vender un fondo, ir a `PuzzleCatalog > Fondos de pantalla`, mantener su `Id` permanente, asignar la imagen y poner **Precio en estrellas** mayor que cero. El fondo aparecerá en la pestaña de fondos de la tienda. Una compra lo desbloquea en la galería y permite usarlo como fondo del menú. Si ya se ganó por historia o es el fondo inicial, no se cobra. Un precio de cero conserva el sistema de fondo inicial y recompensas por subcapítulo.

Para vender un puzle o desafío extra, añadirlo a `PuzzleCatalog > Chapters`, asignarle un **Id** único y permanente, su título, imágenes por dificultad y **Precio en estrellas** mayor que cero. Se ofrece en **Puzles y desafíos**; una compra desbloquea sus dificultades con imagen asignada y permite repetirlo desde la tienda, con sus récords y recompensas habituales. No es necesario añadir los desafíos independientes a `Story`. Completar un desafío comprado no adelanta la historia ni desbloquea otros artículos de pago. Precio cero conserva el acceso habitual de la historia.

El botón **Tienda**, el saldo, las seis filas reutilizables, sus textos y el mensaje de recompensa viven en los Canvas de las escenas. Se pueden editar en el Inspector. `Tools > One Hand Puzzle > Add Star Economy to Canvas` añade estos controles a escenas antiguas. `Validate Star Economy` comprueba premios, compras, migración y las referencias de la tienda.

## Crear conversaciones

Crear un asset con `Create → One Hand Puzzle → Dialogue`.

### Fondo y música

La música también puede cambiar varias veces durante la conversación. En cada intervención de `Lines`, **Música de fondo** permite elegir **Conservar música actual** (predeterminado), **Cambiar música** o **Detener música**. Para cambiarla, seleccionar **Cambiar música** y arrastrar un `AudioClip` a **Pista de música**. La nueva pista se reproduce en bucle con los fundidos existentes y se mantiene hasta otro cambio. **Detener música** hace un fundido a silencio; las intervenciones posteriores lo conservan hasta asignar otra pista. El campo `Music` general sigue siendo la pista inicial.

Ejemplo: `Music` = pista tranquila; en la intervención del tropiezo, **Cambiar música** = pista de sorpresa; al terminar el susto, **Detener música**; al entrar al baño, **Cambiar música** = pista relajante. Puede hacerse dentro de una sola conversación y combinarse con cambios de fondo y efectos de sonido. Los cambios musicales se aplican una vez por intervención y no se repiten al paginar, completar el texto u ocultar el cuadro. Repetir la conversación recupera la pista inicial; terminarla, saltarla o salir detiene su música. Las conversaciones existentes conservan su comportamiento.

El fondo también puede cambiar dentro de la misma conversación. En `Lines`, cada intervención tiene **Fondo**: asignar una textura cambia el escenario al comenzar esa intervención; dejarlo vacío conserva el anterior. El cambio es inmediato, mantiene la proporción de la imagen y no altera el reparto, las expresiones ni la música. El `Background` de la conversación sigue siendo su fondo inicial. Al repetir o iniciar otra conversación se restablece su propio fondo inicial.

Por ejemplo, una sola conversación puede empezar en el cuarto, cambiar a las escaleras en la línea donde Shouta baja y pasar al baño en otra línea. Todo puede pertenecer a un único subcapítulo. Además, un subcapítulo ya admite varios pasos de conversación: su `First Step Id` y `Last Step Id` delimitan el conjunto, sin necesidad de crear un subcapítulo por escenario.

Cada intervención también ofrece **Efecto de sonido** (`AudioClip`) y **Volumen del efecto** (0–1). Para una caída, asignar el clip a la intervención que describe el golpe. Se reproduce al entrar en esa intervención, una sola vez, incluso si el texto se divide en varias páginas. Completar el texto y ocultar o restaurar el cuadro no lo repiten. El volumen respeta las opciones de efectos y no modifica la música. Los efectos pueden terminar durante las líneas siguientes; terminar, saltar o cancelar la conversación los detiene. Los campos vacíos conservan el comportamiento de las conversaciones existentes.

Para usar un archivo nuevo, importarlo en `Assets/Audio` (o la carpeta de audio del proyecto) y arrastrarlo desde Project al campo **Efecto de sonido**. No es necesario añadir objetos al Canvas. La comprobación `Tools > One Hand Puzzle > Validate Dialogue Backgrounds and Sounds (Play Mode)` verifica cambios de fondo, paginación, efectos y compatibilidad.

`Background` es el fondo de esa conversación. Tiene prioridad sobre la imagen que pueda pasarle el controlador. En el flujo normal de `Story` no se proporciona un fondo alternativo: asignarlo en cada conversación cuando se quiera una imagen de fondo. El fondo se recorta para cubrir la pantalla conservando la proporción.

`Music` acepta un `AudioClip` y se reproduce en bucle. Dejarlo vacío produce silencio de fondo, con un fundido de salida si había una pista anterior. No se hereda automáticamente la música del puzzle ni de la conversación anterior.

### Reparto e intervenciones

1. Añadir los assets de personajes a `Characters`. Los dos primeros personajes distintos con retrato ocupan inicialmente los dos puestos. Los personajes sin retrato pueden hablar, pero no reservan puestos ni sustituyen a los personajes visibles.
2. Añadir las intervenciones en `Lines`.
3. En cada intervención, asignar `Character` y escribir `Text`.
4. Para narración, dejar `Character` y `Speaker` vacíos y usar `Speaker Side = Narration`.

El nombre se toma del personaje asignado. Las líneas vacías se omiten y los textos largos se dividen automáticamente en páginas. No es necesario insertar una línea por cada página visual.

El texto de diálogo usa tamaño 34 en el Canvas. Cada página muestra como máximo cuatro líneas, sin reducir automáticamente la letra; un texto más largo continúa al avanzar. El límite se ajusta en `Max Lines Per Page` del `DialogueController`, y el tamaño de letra y de la caja siguen siendo editables en el Canvas.

En cada línea, activar `Pensamiento` para el monólogo interior del personaje. Por defecto usa cursiva y azul claro. La siguiente intervención sin esa opción recupera el estilo hablado. En el Canvas, `Content/Dialogue Box/Body` contiene el componente `Dialogue Text Style`, donde se pueden editar los colores y estilos de letra de ambos modos. Los pensamientos largos conservan su estilo en todas sus páginas, respetando el límite de cuatro líneas.

Activar `Enter On First Speech` para comenzar sin retratos: cada personaje entra desde el lateral cuando habla por primera vez. El primero con retrato ocupa la izquierda y el segundo la derecha; las voces sin retrato no cuentan. Una vez visible, el personaje permanece mientras escucha y conserva su expresión hasta que se cambie explícitamente mediante `Expression` o `Reactions`. La visibilidad utiliza el sprite del outfit y la expresión activos.

Si hay tres o más personajes, el que entra sustituye al ocupante cuyo siguiente turno quede más lejos. Quien no vuelve a hablar tiene prioridad para salir. En caso de empate se sustituye al oyente. Un personaje mencionado mediante `Character` en una línea se incorpora al reparto aunque no se haya añadido expresamente a `Characters`; declarar el reparto completo permite controlar mejor la pareja inicial.

Los personajes salen por el lateral que ocupan y sus sustitutos entran por ese mismo lateral. El personaje que escucha se muestra al 92 % de escala y con un tono más oscuro por defecto. Las animaciones y el encuadre se ajustan en el `DialogueController` de las escenas correspondientes.

### Expresiones y reacciones

En el personaje, añadir a `Expressions` las variantes del retrato predeterminado, por ejemplo `feliz`, `triste` y `enfadado`. En cada outfit, añadir a su propia lista `Expressions` los sprites correspondientes a ese vestuario. Utilizar los mismos identificadores en los distintos outfits para conservar una expresión al cambiar de ropa. Cada expresión utiliza un sprite completo, con el mismo encuadre, tamaño de lienzo y orientación que el retrato de su vestuario. `default` está reservado para la expresión predeterminada, representada por `Portrait`.

La conversación puede configurar `Initial Expressions` por personaje. En cada intervención, `Expression` selecciona la expresión de quien habla. **Conservar expresión actual** la mantiene y **Predeterminada (restablecer)** vuelve a `Portrait` del outfit actual, conservando la ropa.

`Reactions` es una lista de cambios de expresión para otros personajes en esa misma intervención. Asignar el `Character` objetivo y elegir su `Expression`. No depende de su puesto izquierdo o derecho ni le obliga a entrar en pantalla; el personaje debe estar declarado en `Characters` o participar mediante `Character` en alguna línea. También se pueden incluir reacciones en una línea de narración.

| Intervención | Quién habla | `Expression` | `Reactions` |
| --- | --- | --- | --- |
| 1 | Personaje A | `feliz` | Vacío |
| 2 | Personaje B | `enfadado` | Personaje A → `triste` |
| 3 | Personaje A | Conservar expresión actual | Vacío |

En la tercera intervención, A sigue triste y B sigue enfadado. Las expresiones se conservan durante toda la conversación, también al salir y volver a entrar y entre las páginas de un texto largo. Cada conversación y cada repetición comienza de nuevo con sus outfits y expresiones iniciales; no se modifican los assets de personajes.

Los desplegables muestran los identificadores disponibles en el personaje. El Inspector comprueba que la combinación con el outfit activo tenga sprite y señala IDs repetidos, sprites vacíos y cambios contradictorios para el mismo personaje. Si falta una expresión para un outfit, se emite un aviso y se muestra el retrato predeterminado de ese outfit, conservando la expresión solicitada para cuando vuelva a un outfit compatible. Cambiar outfit y restablecer expresión en la misma línea evita esa combinación incompleta. `Tools > One Hand Puzzle > Validate Dialogue Expressions` comprueba expresiones simultáneas de hablante y oyente, persistencia, vestuario, reentrada y paginación.

### Campos manuales compatibles

Para conversaciones sin reparto siguen disponibles `Left Character`, `Right Character`, sus orientaciones y los campos `Speaker` y `Speaker Side` de cada línea. Las líneas pueden sustituir un sprite o usar `Hide Left Character` y `Hide Right Character`.

En el modo manual, un sprite vacío conserva el anterior y un campo de ocultación lo retira. Con reparto automático, esos campos funcionan como sustituciones u ocultaciones visuales de esa intervención. Para contenido nuevo conviene usar los assets de personajes y evitar mezclar ambos sistemas salvo que se busque una sustitución concreta.

### Probar una conversación

1. Abrir `Assets/Scenes/MainMenu.unity` y entrar en Play.
2. Continuar la historia o abrir un subcapítulo disponible en **Capítulos**.
3. Comprobar nombre, páginas, posiciones, orientación, entradas laterales y música.
4. Para revisar una conversación nueva, añadir su paso a `Story.asset` y asignarle un subcapítulo, como se explica más abajo.

## Configurar un puzzle

En `Assets/Data/PuzzleCatalog.asset`, añadir una entrada en `Chapters` y anotar su índice, comenzando por **0**.

| Campo | Función |
| --- | --- |
| `Title` | Nombre del puzzle, utilizado en selección y resultados. La galería muestra solo las imágenes. |
| `Story` | Descripción heredada; actualmente no se muestra al jugador. |
| `Imagen · Fácil (3×3)` | Textura de la dificultad fácil. Su nombre de campo en código sigue siendo `image`. |
| `Imagen · Normal (4×4)` | Textura de la dificultad normal, campo `normalImage`. |
| `Imagen · Difícil (5×5)` | Textura de la dificultad difícil, campo `hardImage`. |
| `Difficulty` | Dificultad que utiliza la historia. El jugador solo puede elegir otra al repetir el subcapítulo. |
| `Music` | Música del puzzle compartida por sus tres dificultades. Vacío significa sin música. |
| `Introduction` y `Conclusion` | Compatibilidad con el flujo antiguo. En la historia con `Story` asignada se utilizan sus pasos `Conversation`. |

Asignar una imagen diferente a cada dificultad. La selección nunca sustituye una imagen ausente por la de otra dificultad: la pestaña existe, pero **Jugar** queda deshabilitado si falta su textura. Si falta la imagen de la dificultad de historia, la validación impide iniciar esa secuencia.

El tablero y las miniaturas utilizan un recorte cuadrado central de la textura. El visor de galería muestra el archivo completo conservando su proporción. Utilizar imágenes cuadradas cuando se quiera que ambos encuadres coincidan exactamente.

El mínimo de movimientos se calcula para cada mezcla generada. No se configura manualmente y no es una propiedad de la ilustración. La nota y el estado completado se guardan por pareja de índice de puzzle y dificultad.

## Configurar la historia lineal

En `Story.asset → Steps`, añadir los pasos en el orden en que debe vivirlos el jugador.

| Campo de un paso | Configuración |
| --- | --- |
| `Id` | Identificador único y permanente, por ejemplo `chapter-1-3`. Se utiliza en el guardado. |
| `Title` | Nombre del lugar o momento que puede aparecer en la vista de continuar historia. |
| `Kind = Conversation` | Asignar `Conversation` al asset de diálogo. `Puzzle Index` no se utiliza. |
| `Kind = Puzzle` | Indicar en `Puzzle Index` la posición de su definición en el catálogo. `Conversation` no se utiliza. |

Se permiten conversaciones consecutivas, por ejemplo llegada a la casa, conversación en el salón y después puzzle. Cada conversación puede tener su propio fondo, reparto y música.

Mantener los identificadores de los pasos ya publicados. Si se cambia un identificador guardado, puede dejar de encontrarse el punto de continuación. Mantener también los índices de puzzles existentes: se usan en los pasos y en las notas guardadas. Para ampliar el catálogo, añadir entradas al final.

## Configurar capítulos y subcapítulos

En `Story.asset → Chapters`, añadir los capítulos narrativos con su `Title`. En cada uno, añadir los `Subchapters`:

| Campo del subcapítulo | Uso |
| --- | --- |
| `Title` | Texto del botón en la selección. |
| `First Step Id` | Id del primer paso que se repetirá. |
| `Last Step Id` | Id del último paso incluido. Para una conversación suelta, usar el mismo Id en ambos campos. |

Los rangos son continuos e inclusivos. No pueden solaparse entre subcapítulos, tampoco entre capítulos. Cada rango puede contener cero o un puzzle. Si una parte de la historia tiene dos puzzles, dividirla en dos subcapítulos.

Los pasos sin agrupar siguen ejecutándose en la historia, pero no tendrán una entrada propia para repetirse. Conviene cubrir todos los pasos con rangos si se quiere que todo el contenido sea accesible desde la biblioteca.

### Reglas de desbloqueo

- Un subcapítulo de conversación aparece al alcanzar su primer paso.
- Un subcapítulo con puzzle aparece al alcanzar su paso de puzzle. Haber visto solo su conversación introductoria todavía no lo muestra en la biblioteca.
- Un capítulo aparece cuando tiene al menos un subcapítulo desbloqueado. No se dibujan botones de capítulos ni subcapítulos futuros bloqueados.
- La repetición termina como máximo en el punto más lejano alcanzado en la historia. Una conversación de cierre aún no alcanzada no se reproduce por resolver antes ese puzzle desde la biblioteca.
- Los desbloqueos anteriores se conservan cuando se repite la historia desde el principio.

## Añadir un capítulo con introducción puzzle y cierre

Ejemplo: añadir un segundo capítulo, **El bosque**, con un puzzle nuevo.

1. Crear `BosqueIntro` y `BosqueCierre` como assets de diálogo. Asignar personajes, texto, fondo y música opcional a cada uno.
2. Añadir la definición del puzzle al final de `PuzzleCatalog → Chapters`. Como el catálogo actual tiene uno, su índice será **1**.
3. Asignar las tres imágenes de dificultad, un título, la música opcional y la dificultad de historia; por ejemplo, `Normal`.
4. Añadir estos tres pasos al final de `Story → Steps`:

| Id | Kind | Conversation | Puzzle Index |
| --- | --- | --- | --- |
| `bosque-intro` | Conversation | BosqueIntro | No se utiliza |
| `bosque-puzzle` | Puzzle | No se utiliza | 1 |
| `bosque-cierre` | Conversation | BosqueCierre | No se utiliza |

5. Añadir un capítulo **El bosque** en `Story → Chapters`.
6. Añadir un subcapítulo **El recuerdo del bosque**, con `First Step Id = bosque-intro` y `Last Step Id = bosque-cierre`.
7. Revisar `Ending Text` si el texto final debe cambiar.
8. Probar desde una partida que pueda alcanzar esos pasos. La historia abrirá el tablero 4×4 con la imagen normal del nuevo puzzle.
9. Comprobar la repetición, las tres pestañas, las notas separadas y las entradas de galería al completar cada dificultad.

Para insertar una conversación de llegada independiente, añadir antes un paso `bosque-llegada` y darle otro subcapítulo cuyo primer y último Id sean ese mismo valor. Para un capítulo solo de conversación, omitir la entrada de puzzle en el catálogo y agrupar únicamente pasos `Conversation`.

## Organización actual del juego

| Capítulo | Subcapítulo | Primer paso | Último paso | Puzle |
| --- | --- | --- | --- | --- |
| Capítulo 1 | 1-1 | chapter-1-1 | chapter-1-1 | Ninguno |
| Capítulo 1 | 1-2 | chapter-1-2 | chapter-1-2-puzzle | Índice 0, fácil en historia |
| Capítulo 1 | 1-3 | chapter-1-3 | chapter-1-3 | Ninguno |

El catálogo contiene únicamente el puzle `1-2`, con las imágenes `1-2_Easy`, `1-2_Normal` y `1-2_Hard`. Las conversaciones `1-4`, `1-4 Cierre`, `1-5`, `1-6` y `1-6 cierre` están escritas en `Assets/Data/Dialogue/Chapter 1`, pendientes de enlazar a la historia. El reparto actual es Shouta, Ayame y el padre de Shouta.

## Ajustar la presentación

Las conversaciones y los puzles comparten la paleta de los menús: superficies azul oscuro, texto crema y acentos dorados. El diálogo conserva cuatro líneas por página, los pensamientos en azul y cursiva, el clic para completar o avanzar y los controles **Ocultar**, **Saltar** y **Menú principal**. Sus colores y posiciones se editan en `Dialogue/Content/Dialogue Box`; el estilo de pensamiento está en `Body > DialogueTextStyle`.

En las tres escenas de puzle, `Puzzle Layout` contiene la cabecera con el saldo de estrellas, el tablero central, el contador de movimientos y las acciones inferiores. `Puzzle Complete/Content` contiene la imagen, las estrellas, el récord, la recompensa y los botones de continuación. La imagen de resultado se ajusta dentro de `Artwork/Image`. La pantalla de fin de historia utiliza la misma presentación. `Tools > One Hand Puzzle > Style Dialogue and Puzzle Screens` permite volver a aplicar el estilo inicial; sobrescribe los ajustes visuales de estas cuatro escenas. El motor utiliza los controles ya guardados en sus Canvas.

Los elementos estáticos de la interfaz están guardados en los Canvas de las escenas. En `MainMenu`, expandir `Main Menu Canvas` para editar título, menú principal, galería, visor, selección de historia, ajustes y recapitulación. Cada panel tiene sus controles dentro de `Content`; el visor utiliza `Viewport` y `Controls`. Activar temporalmente un panel en el editor permite ajustar posiciones, tamaños, colores y textos. Al iniciar el juego, el controlador activa la pantalla correspondiente.

Los textos fijos se editan en el componente `Text` de cada objeto. Los textos que cambian de estado o muestran valores (ajustes, fondo seleccionado, movimientos, notas y paginación) tienen además un componente `CanvasText`: editar su lista **Textos por estado**, conservando los marcadores `{0}`, `{1}`, etc. para los valores del juego. Estas variantes se guardan en la escena y el código respeta su contenido. Los títulos de capítulos y las conversaciones se editan en sus assets de catálogo e historia.

Las escenas `PuzzleEasy`, `PuzzleNormal` y `PuzzleHard` contienen los botones del tablero, las capas de revelado, el panel de resultados y el diálogo. `Story` contiene el diálogo y los paneles de final y error. El código actualiza imágenes, valores, visibilidad y acciones sin reconstruir estos controles. Mantener los nombres de los objetos y las referencias serializadas al editar la jerarquía. `Tools > One Hand Puzzle > Validate Canvas References` comprueba las cinco escenas sin modificar las escenas abiertas.

`Fondo inicial` del catálogo es el fondo del menú al comenzar una partida. Añadirlo también a `Fondos de pantalla` permite volver a seleccionarlo desde la galería. Registrar en esa lista todas las imágenes que puedan utilizarse como fondo, cada una con un `Id` único y permanente.

En `Story → Chapters → Subchapters → Fondos al completar el subcapítulo`, arrastrar una o varias imágenes registradas en el catálogo. Se desbloquean cuando el jugador termina el último paso de ese subcapítulo, incluido su último puzzle o diálogo. Dejar la lista vacía no concede fondos. Los fondos pendientes no aparecen en la galería ni pueden seleccionarse. Los ganados se guardan por identificador de juego y de fondo, sobreviven a repetir la historia y se conceden también a partidas anteriores que ya hayan completado el subcapítulo.

La galería separa `Puzles` de `Fondos de pantalla`. Solo estos últimos ofrecen **Usar como fondo**; la elección se guarda para las siguientes sesiones. La vista ampliada de ambas categorías dispone de zoom de 1× a 4× y arrastre cuando la imagen supera el tamaño de pantalla.

Para conceder un fondo al terminar una parte concreta, usar `Story → Chapters → Subchapters → Fondos al completar el subcapítulo`. Por ejemplo, asignar el coche como `Fondo inicial` y la entrada de día como recompensa del subcapítulo `1-1`: al comenzar solo estará disponible el coche y al terminar `1-1` se añadirá la entrada, sin esperar al resto del capítulo. Cada subcapítulo puede conceder uno o varios fondos.

Las escenas utilizan una resolución de referencia de Canvas de 900×1000. Los menús se extienden al área segura de la pantalla, con cabecera y navegación inferior ancladas a sus bordes. El cuadro de conversación y el visor de galería también se anclan al tamaño de pantalla.

En `DialogueController` se pueden ajustar velocidad base, opacidad, escala del oyente, duración del cambio de foco y duración de entrada y salida. La posición y distancia al borde de los personajes se editan en los RectTransform `Dialogue/Content/Left Character` y `Right Character` de cada Canvas. Los ajustes guardados de opacidad y velocidad del jugador pueden modificar el resultado visible.

Los retratos utilizan la altura de esos RectTransform; su ancho se adapta a la proporción de cada sprite. Así, una imagen más ancha no reduce la altura del personaje al cambiar de expresión. Mantener el mismo encuadre vertical entre los sprites de un personaje; la reducción de escala del oyente se aplica después.

En el componente `Puzzle` de cada escena se configuran `Slide Duration`, `Completion Flash In Duration`, `Completion Flash Out Duration` y `Completed Image Hold Duration`. Sus valores por defecto son 0,16 s, 1 s, 1,1 s y 0,9 s, respectivamente. Son configuraciones de escena: revisar las tres dificultades al cambiarlas.
