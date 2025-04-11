# GpsTracerRelay


## Intro
GpsTracerRelay is a small tool for playing GPS track, also this tool creates track from file containing key points and 
creates output file containing all necessary points filling the route with missing points. 
This tool emulate Topin ZX303 tracker

##Usage

### Play mode
Usage: GpsTracerRelay config file tracks file
example: `GpsTracerRelay settings.xml track.xml`
### Create mode
Or usage: GpsTracerRelay -g source_file dist_file interval in second
Example:  `GpsTracerRelay -g src.xml dst.xml 30`