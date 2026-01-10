# Overdrive Car Hardware
This is another writeup of the Anki Overdrive car specifications, mostly for preservation safety.  
Major Credits To:  
[Anki Overdrive Fandom Wiki](https://anki-overdrive.fandom.com/wiki/Specifications)  
[Micro Controller Tips Hardware Breakdown](https://www.microcontrollertips.com/teardown-inside-anki-overdrive-racecar-set/)

## Car Disassembly
1. Remove screws from the bottom of the car.
2. Remove the upper shell.
3. To separate the PCB from the lower shell, carefully pry the PCB from the shell, it is held in place by 4 clips at the front and back corners.
4. Now you can remove the PCB and battery from the lower shell.  
For Battery Replacement, see [Battery Replacement](#battery-replacement)
5. Remove weight from back of the car to access the motors, they just pop out.
6. The power contacts can also be removed (they can be very loose so be careful not to lose them)  
I am unsure at this time if it is possible to dissasemble the gearbox.

## Car hardware

For transparency, I don't have much experience with electronics so I am simply listing the components that are known.  

The cars have a rgb led on the top of the pcb, and 2 for back and 2 for the front.  
Also the `Linear Technology LTC4054` is used for charging the battery. 

The underside of the PCB is more complex, featuring the contacts for the charging bridge, 2 beam sensors to measure the current wheel speed.
Also found are the `Nordic Semi RF8001 BLE` module, the `STM32F051K8` microcontroller and motor controller, a `Philips BF547 npn 1GHz wideband transistor`
and some `Diodes Inc. DMG1016V` mosfets.



### Battery Replacement
1. Desolder the old battery
2. Solder in a new battery
3. Ensure the motor wires are routed through the slots at the back of the PCB
4. Reassemble the car (Be careful with the clips, they are fragile)

### ChargeFlash
the cars let you flash them using the charging pins using a modified charger like this:
UART Tx Pin -> Base of NPN Transistor
UART 5V Pin -> NPN Transistor collector
Charger Positive -> NPN Transistor emitter
Charger Ground -> UART Ground


[Back To Root](https://github.com/MasterAirscrachDev/Anki-Partydrive?tab=readme-ov-file#anki-partydrive)