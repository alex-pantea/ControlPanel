# Control Panel

Control Panel is an open-source input/output device with software-configurable app selection to control either app or system volume levels.

A motorized linear fader (or slide pot) is used for both input and output methods, with capacitive touch support as well.

This is still a WIP platform, with features planned to incorporate a Teensy with ethernet to allow API access.
This would allow integrating into projects such as [home assistant](https://www.home-assistant.io/) for control of more than just volume levels.

## Designs
### Control Panel V1
Prototype device to experiment functionality and use.

#### Demo Video
<a href="https://vimeo.com/759258811">
    <img src="https://i.imgur.com/w9uoxgJ.jpg" width="480" />
</a>

Currently using proto-board with handwiring for the prototype, and inefficient L298N motor driver.

### Control Panel X4
Planned for future, using 4 motorized faders.

## Firmware

V1 version supports:
- capacitive touch sensor on fader cap
- single motorized fader
- positioning fader using serial port

## Software

V1 version supports:
- layer switching through double or triple click (using the capacitive sensor)
- controlling system volume
- controlling app volume
- controlling remote volume through SSH (rooted iPhone)
- player/track monitoring through [YouTube Music Desktop - Remote Control](https://ytmdesktop.app/)

## TODO
- Add RGB (duh)
- Migrate to Teensy 4.1
  - Look into using ESPHome for native WiFi support as an alternative
- Use API instead of serial port for communication
- Design PCB
- Add license

## Acknowledgements

Originally got the concept idea from the [SmartKnob](https://github.com/scottbez1/smartknob) project, but wanted to use motorized linear faders.
Just like you'd find on [MIDI control surfaces](https://www.sweetwater.com/store/detail/XTouch--behringer-by-touch-universal-control-surface) or the [GoXLR](https://www.tc-helicon.com/product.html?modelCode=P0CQK).

Only later did I find out about the [PCPanel-Pro](https://www.getpcpanel.com/product-page/pcpanel-pro), which has some common functionality but doesn't have motorized faders. The UI from this served as great inspiration.
