# Erostek Support for Buttplug

The buttplug schema does not define E-Stim specific messages yet, and thus there are no applications yet targeting E-Stim devices via buttplug. To solve this chicken-and-egg situation and make E-Stim support in buttplug useful from the start, we will be emulating the stroking motions of a Fleshlight Launch for now. This makes synched E-Stim available for all media supporting the Launch, and to a large degree also to those supporting the Kiiroo Onyx.

Simulation will alternate between Channel A and B simulating a stroking movement. The stim level will fade out when there is no movement, and will fade in for a few seconds when movement starts again to avoid unpleasurable situations. The current algorithm is still very basic. Stay tuned for future releases.

## Usage

Power up your device and turn both level pots to 0%. Connect your device to a free serial port, USB-to-serial or bluetooth-to-serial adapter using the link cable. Start your buttplug-enabled application, select "Start Scanning" in the device tab, and your device should show up in the list within 10 to about 60 seconds.

Select the device, and you're good to go. Control the intensity by using the frontpanel Channel A and Channel B knobs. Use the MA knob to adjust the stim frequency. Pulse width will be taken from the "Width" parameter in your advanced settings.

You can connect more than one box if you want to.

### Suggested Electrode Placement

- Channel A -> Base
- Channel B -> Tip
- Common -> Anal

## Link Cable

If you don't own the original overpriced "ErosLink" cable, you can easily build your own. The link cable consists of a 3.5mm TRS (stereo audio) jack, going to some sort of computer connection, be it Female DB-9 or a RS232-to-USB converter. The pin connections are as follows:

    3.5mm Tip <-> RX (DB-9 Pin 2)
    3.5mm Ring <-> TX (DB-9 Pin 3)
    3.5mm Sleeve <-> Ground (DB-9 Pin 5)

## Supported Devices

- ErosTek ET312
- ErosTek ET312-B
- MK-312BT

## Known Issues

- Box will stay in buttplug mode after application has quit.
- Box may have to be power cycled after certain disconnection scenarios before being able to reconnect.
- Buttplug does not allow user configurable settings yet, so it's not possible to set the COM port to a fixed value yet.
- Serial Port probing may cause other devices connected to COM ports to misbehave.

## Future development

- Add E-Stim specific messages to buttplug-schema
- Add user configurable COM port settings
- Add device control GUI
- Improve Fleshlight Launch emulation algorithm
