import asyncio
from bleak import BleakClient, BleakScanner
from bleak.backends.characteristic import BleakGATTCharacteristic
from bleak_winrt.windows.devices.bluetooth.genericattributeprofile import GattCharacteristicProperties
from bleak.backends.scanner import AdvertisementData
from bleak.backends.device import BLEDevice
from bleak_winrt.windows.storage.streams import DataWriter, DataReader
import logging
import winrt.windows.devices.bluetooth as winbt
import winrt.windows.devices.bluetooth.genericattributeprofile as gatt

# Set up logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(message)s')
logger = logging.getLogger(__name__)

# Define UUIDs
SUPERCAR_SERVICE_UUID = "BE15BEEF-6186-407E-8381-0BD89C4D8DF4"
SUPERCAR_READ_CHAR_UUID = "BE15BEE0-6186-407E-8381-0BD89C4D8DF4"
SUPERCAR_WRITE_CHAR_UUID = "BE15BEE1-6186-407E-8381-0BD89C4D8DF4"

class SuperCarEmulator:
    def __init__(self):
        self.real_supercar = None
        self.emulated_supercar = None

    async def find_real_supercar(self):
        devices = await BleakScanner.discover()
        for device in devices:
            if device.name and device.name.endswith("Drive"):
                logger.info(f"Found real SuperCar: {device.name} - {device.address}")
                return device
        logger.error("No real SuperCar found. Make sure it's powered on and in range.")
        return None

    async def connect_to_real_supercar(self, device):
        self.real_supercar = BleakClient(device)
        await self.real_supercar.connect()
        logger.info(f"Connected to real SuperCar: {device.name} - {device.address}")

    async def setup_emulated_supercar(self):
        # Create a BluetoothLEAdvertisementPublisher
        publisher = winbt.advertisement.BluetoothLEAdvertisementPublisher()
        publisher.advertisement.local_name = "EmulatedSuperCarDrive"

        # Add the SuperCar service UUID to the advertisement
        data_section = winbt.advertisement.BluetoothLEAdvertisementDataSection()
        data_section.data_type = 0x07  # Complete list of 128-bit Service UUIDs
        writer = DataWriter()
        writer.write_guid(SUPERCAR_SERVICE_UUID)
        data_section.data = writer.detach_buffer()
        publisher.advertisement.data_sections.append(data_section)

        # Start advertising
        publisher.start()
        logger.info("Started advertising as EmulatedSuperCarDrive")

        # Create a GattServiceProvider for the SuperCar service
        service_provider = await gatt.GattServiceProvider.create_async(SUPERCAR_SERVICE_UUID)
        service = service_provider.service

        # Add characteristics
        read_char = await service.create_characteristic_async(
            SUPERCAR_READ_CHAR_UUID,
            GattCharacteristicProperties.READ | GattCharacteristicProperties.NOTIFY
        )
        write_char = await service.create_characteristic_async(
            SUPERCAR_WRITE_CHAR_UUID,
            GattCharacteristicProperties.WRITE
        )

        # Set up read and write handlers
        read_char.read_requested = self.handle_read_request
        write_char.write_requested = self.handle_write_request

        self.emulated_supercar = service_provider
        logger.info("Emulated SuperCar service set up successfully")

    async def handle_read_request(self, sender, args):
        # Read from the real SuperCar and respond
        try:
            data = await self.real_supercar.read_gatt_char(SUPERCAR_READ_CHAR_UUID)
            logger.info(f"Read from real SuperCar: {data.hex()}")
            writer = DataWriter()
            writer.write_bytes(data)
            args.set_value(writer.detach_buffer())
            args.status = gatt.GattReadRequestStatus.SUCCESS
        except Exception as e:
            logger.error(f"Error reading from real SuperCar: {str(e)}")
            args.status = gatt.GattReadRequestStatus.UNLIKELY_ERROR

    async def handle_write_request(self, sender, args):
        # Write to the real SuperCar
        try:
            reader = DataReader.from_buffer(args.value)
            data = reader.read_bytes(reader.unconsumed_buffer_length)
            logger.info(f"Writing to real SuperCar: {data.hex()}")
            await self.real_supercar.write_gatt_char(SUPERCAR_WRITE_CHAR_UUID, data)
            args.status = gatt.GattWriteRequestStatus.SUCCESS
        except Exception as e:
            logger.error(f"Error writing to real SuperCar: {str(e)}")
            args.status = gatt.GattWriteRequestStatus.UNLIKELY_ERROR

    async def run(self):
        # Find and connect to the real SuperCar
        real_supercar_device = await self.find_real_supercar()
        if not real_supercar_device:
            return

        await self.connect_to_real_supercar(real_supercar_device)

        # Set up the emulated SuperCar
        await self.setup_emulated_supercar()

        # Keep the program running
        while True:
            await asyncio.sleep(1)

async def main():
    emulator = SuperCarEmulator()
    await emulator.run()

if __name__ == "__main__":
    asyncio.run(main())
