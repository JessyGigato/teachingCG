# Introducción a la Computación Gráfica

El proyecto teachingCG es un repositorio con clases y materiales que dan soporte al curso Introducción a la Computación Grafica. El proyecto principal es una aplicación de consola Renderer (implementada en C++ y C#) que al ejecutar sintetiza una imagen haciendo uso de alguna técnica aplicada a algún modelo.

El resultado es uno o varios ficheros que describen la imagen de forma binaria (`.rbm` raw bitmap). El formato del fichero es binario y presenta un float con el ancho, un float con el alto y a continuación 4 floats por cada píxel describiendo la intensidad (r,g,b,a).

La idea es que el proyecto Renderer sea lo más independiente de plataforma posible. No obstante, dos proyectos presentes en el repositorio permiten inspeccionar el tipo de fichero propuesto.

- ImageViewer\C# (utilizando WindowsForms)

- ImageViewer\Python (utilizando matplotlib)

El proyecto master evoluciona en consecuencia con cada conferencia del curso. Para cada conferencia existirá una branch con el estado del proyecto ajustado al tema en cuestión.