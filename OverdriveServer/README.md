# Overdrive Server Documentation
For the Anki Overdrive Bluetooth API see [Overdrive BlootoothLE Protocol](#anki-overdrive-bluetooth-api)

Overdrive Server is a server that can connect to Anki Overdrive cars and control them. It is written in C# and uses the [32feet.NET](https://inthehand.com/components/32feet/) library to connect to the cars over Bluetooth LE.

Overdrive Server has a basic web interface that can be used to control the cars. 
`http://localhost:7117`

to recive data from the server in an app, first call  
`http://localhost:7117/registerlogs` and then you can poll  
`http://localhost:7117/logs` and `http://localhost:7117/utillogs`  
logs gives any alerts for the developer, utillogs will give formatted events to be parsed by the app.

### Unless specified, all endpoints return status 200 and a string with the result of the operation.

#### Control a car (Set Speed and lane)
`http://localhost:7117/controlcar/{carID:speed:lane}`

#### Set the track center (tells the car that it is in the center of the track)
`http://localhost:7117/resetcenter/{carID}`

#### Scan for cars
`http://localhost:7117/scan`

#### Cars (gets the list of cars in json format)
`http://localhost:7117/cars`
```json
"CarData": [
    {
        "name": null,
        "id": null,
        "trackPosition": 0,
        "trackID": 0,
        "laneOffset": 0.0,
        "speed": 0,
        "battery": 0,
        "charging": false
    }
]
```
#### Retrieve the battery levels of the cars
`http://localhost:7117/batteries`

#### Set the Light color of a car
`http://localhost:7117/setlight/{carID:R:G:B}`
- R, G, B are floats between 0 and 1

#### Clears The Car Data (this shoudnt be needed)
`http://localhost:7117/clearcardata`

#### Play Text To Speech using Windows TTS
`http://localhost:7117/tts/{text}`

#### Initate a track scan
`http://localhost:7117/scantrack/{carID}`
- This will cause the car to drive around the track and send back the track data

#### Cancel a track scan
`http://localhost:7117/canceltrackscan/{carID}`

#### Force a car to disconnect
`http://localhost:7117/disconnectcar/{carID}`

#### Get the track data as json
`http://localhost:7117/track`
```json
```

[Back To Root](https://github.com/MasterAirscrachDev/Anki-Partydrive?tab=readme-ov-file#anki-partydrive)