# Summary #
This is a tool for creating building layouts for Ubisofts Anno-series.

Currently most layouts are created either by some kind of spreadsheet (excel, google-docs, ..) or directly with image editing. The target of this project ist to supply users with an easy method to create good-looking, consistent layouts without having to think about how to do it.

Feel free to leave comments or suggestions on [my talk page at wikia](http://anno2070.wikia.com/wiki/User_talk:ZackSchneider) or submit issues at this project.

# Technology #
This application is written in C# (.NET 4) and uses WPF.

The building presets and icons are extracted from game files using the RDA explorer and a custom script written by Peter Hozak. See the development pages at wikia: http://anno2070.wikia.com/wiki/Development_Pages

If you want to use custom icons or use icon sets for any other Anno-title than 2070 just replace the files within the icons folder.

# Progress #

## Current features ##
  * place, remove, rotate and copy objects
  * supported object properties: size, color, label, icon, influence radius
    * please note that the influence ranges aren't exactly correct, somehow the ingame range is a bit smaller
  * additional options: borderless, road (used for statistics)
  * save and load layouts (.ad)
  * export layouts to image (.png)
  * collision detection to prevent invalid placement of objects
  * online version check
  * local building presets database (v0.5)
  * customizable color presets file
  * optional extension registration (.ad) including file icon
  * calculate statistics (currently bounding box, minimum area and space efficency)
  * fixed the **Windows XP** crash! :)

## Updates with the next version ##
  * include buildings from the addon
  * more stats
  * material cost summation
  * localization to some/all languages of anno2070:
    * eng, ger, fra, esp, ita, pol, rus, cze

## Possible future features ##
  * add online access to building presets database
  * add option to draw the grid on top of the buildings, because sometimes it's hard to see the size of a building without the grid
  * functionality to change properties of selected objects
  * layout validation, like checking for disconnected buildings or, if enough information is present, check if the existing building-connections make any sense
  * automatic coloring according to suited color presets, i.e. color presets which take color-blindness and distinguishability in grayscale into account (see http://anno2070.wikia.com/wiki/Colour_Blindness)
  * calculation of coverage percentage for housings or efficiency percentage for production buildings

## Screenshot ##

![http://anno-designer.googlecode.com/svn/wiki/images/Screenshot10.png](http://anno-designer.googlecode.com/svn/wiki/images/Screenshot10.png)

## Export example ##

![http://anno-designer.googlecode.com/svn/wiki/images/Export8.png](http://anno-designer.googlecode.com/svn/wiki/images/Export8.png)