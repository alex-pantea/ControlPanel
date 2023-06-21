# Control Panel

Control Panel is an open-source input/output device with software-configurable app selection to control either app or system volume levels.

A motorized linear fader (or slide pot) is used for both input and output methods, with capacitive touch support as well.

This is still a WIP platform, based on the popular ESP32 microcontroller. Current support allows for either MQTT or MIDI over BLE communication.

This allows integrating into projects such as [home assistant](https://www.home-assistant.io/) for control of more than just volume levels, or even Digital Audio Workstations (DAWs) for music producers.

## Designs

### Control Panel V1

Prototype device to experiment functionality and use.

#### Demo Video

<a href="https://vimeo.com/759258811">
    <img src="https://i.imgur.com/w9uoxgJ.jpg" width="480" />
</a>

Currently using proto-board with handwiring for the prototype, and inefficient L298N motor driver.

### Control Panel X4

2nd implementation using all 4 available faders.

<a href="https://vimeo.com/838243595">
    <img src="https://i.imgur.com/9qnkagS.jpg" width="480" />
</a>

The first two can be used to adjust audio levels.
With added MQTT support, the other faders can be used for adjusting lighting during conference calls.
Given this is still in it's prototype phase, everything is setup as modularly as possible.
I also modeled and printed an enclosure for the device, and I'm experimenting with vertical and horizontal positioning on my desk.

## Firmware

V1 version supports:

- capacitive touch sensor on fader cap
- single motorized fader
- positioning fader using serial port

X4 version supports:

- asynchronous movement of faders without blocking in case of a jam
- positioning through MQTT
- speed controlled by distance to target through PID loop

## Software

V1 version supports:

- layer switching through double or triple click (using the capacitive sensor)
- controlling system volume
- controlling app volume
- controlling remote volume through SSH (rooted iPhone)
- player/track monitoring through [YouTube Music Desktop - Remote Control](https://ytmdesktop.app/)

X4 version supports:

- cross-platform implementation of volume information using reflection and assembly loading

Breaking Changes:

- removed layer switching temporarily since there's no visual indicator and was not as necessary with 4 faders.
- removed custom SSH scripts
- removed support for YouTube Music Desktop

## TODO

- Incorporate LED indicator(s)
- Push button toggles for extra inputs
- Gather feedback from other users
- Design PCB
- Add license

## Acknowledgements

Originally got the concept idea from the [SmartKnob](https://github.com/scottbez1/smartknob) project, but wanted to use motorized linear faders.
Just like you'd find on [MIDI control surfaces](https://www.sweetwater.com/store/detail/XTouch--behringer-by-touch-universal-control-surface) or the [GoXLR](https://www.tc-helicon.com/product.html?modelCode=P0CQK).

Only later did I find out about the [PCPanel-Pro](https://www.getpcpanel.com/product-page/pcpanel-pro), which has some common functionality but doesn't have motorized faders. The UI from this served as great inspiration.
