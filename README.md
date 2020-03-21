Adds extra seats to the minicopter and horse.

## Configuration

```json
{
  "EnableExtraHorseSeat": false, //adds an extra seat to the horse
  "EnableMiniBackSeat": false, //adds an extra backwards facing rotor seat
  "EnableMiniSideSeats": false //adds a seat on each side of the wheel A frame
}
```

## PLugin Hooks

Note: If you want a plugin to handle all seat creation, then please make sure that the config has all seats set to the default false.

```csharp
		bool OnMiniCanCreateRotorSeat(BaseVehicle entity) {
			return false;
		}
		
		bool OnMiniCanCreateSideSeats(BaseVehicle entity) {
			return false;
		}
		
		bool OnHorseCanCreateBackSeat(BaseVehicle entity) {
			return false;
		}
```