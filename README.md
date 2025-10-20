# Back From Goal (Generador de Niveles Sokoban)

## Descripción general

**Back From Goal** es un generador y simulador de niveles basado en las mecánicas del clásico juego *Sokoban*.  
El jugador debe mover bloques (rocas) para tapar todos los agujeros del escenario, completando el nivel.

**Controles:**
- **W / A / S / D** → Mover al jugador en las cuatro direcciones.  
- **Objetivo:** Cubrir todos los agujeros con rocas.  
- El nivel termina automáticamente cuando todos los agujeros están cubiertos.

El proyecto implementa un sistema de **generación automática de niveles resolubles**, utilizando una técnica de simulación inversa: parte desde una configuración “resuelta” (todas las rocas dentro de los agujeros) y retrocede movimientos válidos para construir un desafío lógico alcanzable por el jugador.

---

## Principio del algoritmo

El núcleo del sistema se encuentra en el script **`BackFromGoalAlgorithm.cs`**, que ejecuta la generación paso a paso:

1. **Generación base del grid**  
   Se crea una cuadrícula de **10x8** (`simGrid`) con:
   - Celdas vacías  
   - Muros  
   - Agujeros (objetivos)  
   - Rocas  
   - Posición inicial del jugador  

2. **Colocación inicial (estado final resuelto)**  
   Las rocas comienzan tapando los agujeros.  
   Este estado se considera la “solución final” del nivel.

3. **Retroceso del estado final (“Back From Goal”)**  
   El algoritmo simula hacia atrás los movimientos que habrían llevado a esa solución:
   - Calcula direcciones válidas alejadas de los agujeros.  
   - Verifica que el jugador pudiera haber empujado esa roca desde una celda adyacente (validado con `CanPlayerReach`).  
   - Evalúa obstáculos, paredes, bordes y esquinas.  
   - Mueve la roca y actualiza la posición del jugador de forma coherente.  
   - Repite el proceso varias veces para “desarmar” la solución final en una configuración jugable.

4. **Evita bucles y retrocesos innecesarios**  
   Se implementa un sistema de registro de movimientos (`lastMove`) para evitar que una roca oscile entre dos posiciones.

5. **Condición de parada**  
   El algoritmo se detiene cuando ninguna roca puede moverse más sin violar las restricciones, o cuando se alcanza el número máximo de pasos definidos (`backwardsSteps`).

---

## Detalles técnicos

- **Lenguaje:** C#  
- **Motor:** Unity  
- **Paradigma:** Programación orientada a objetos  

**Componentes principales:**
- `BackFromGoalAlgorithm`: lógica central del retroceso.  
- `GridManager`: representación y manejo de la grilla del nivel.  
- `LevelRunner`: controla el flujo de generación y ejecución del nivel.  
- `PlayerController`: gestiona los movimientos del jugador (WASD).  
- `CanPlayerReach`: algoritmo de búsqueda de caminos (BFS) que valida si el jugador puede alcanzar una posición detrás de una roca.  
- `MinDistanceToHoles`: calcula la distancia Manhattan mínima entre una roca y los agujeros para priorizar alejamiento.

**Estructura de datos:**
- `GridCellType[,] simGrid` → matriz principal que representa el nivel.  
- `List<Vector2Int> rocksPos` → posiciones de las rocas.  
- `HashSet<Vector2Int> holes` → posiciones de los agujeros.  

**Condiciones de movimiento:**
- El jugador no puede atravesar muros ni otras rocas.  
- Las rocas solo se mueven si el jugador puede empujarlas.  
- No se permiten movimientos que peguen una roca contra un borde o esquina.  
- Se evita repetir el movimiento inverso inmediato (prevención de vaivén).

---

## Ejemplo de flujo

1. Se inicializa la grilla de 10x8 con 4 agujeros y 4 rocas.  
2. Cada roca comienza sobre un agujero (estado resuelto).  
3. El algoritmo retrocede un número determinado de pasos, dispersando las rocas.  
4. El jugador inicia en una posición alcanzable.  
5. Al comenzar el juego, el jugador debe repetir el proceso inverso: empujar las rocas nuevamente sobre los agujeros.

---

## Conceptos implementados

- Generación procedural de niveles resolubles.  
- Simulación inversa (desde la solución hacia el estado inicial).  
- Búsqueda de caminos (BFS) para validar empujes posibles.  
- Prevención de ciclos de movimiento.  
- Sistema de grilla modular y reutilizable.  
- Depuración con logs en consola (`debugSimulation`).  

---

## Posibles mejoras

- Ajustar la dispersión inicial para mayor variedad.  
- Implementar escalado de dificultad según los pasos de retroceso.  
- Agregar detección automática de niveles imposibles.  
- Integrar editor visual de niveles.  
- Incorporar heurísticas de evaluación de dificultad.

---

## Autor

Proyecto desarrollado por **Héctor Villalobos, Matias Oyarzún, Franco Toro** 
Estudiantes de Ingeniería en Desarrollo de Videojuegos y Realidad Virtual — Universidad de Talca.  
