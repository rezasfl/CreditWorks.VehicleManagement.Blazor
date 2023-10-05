﻿using CreditWorks.VehicleManagement.Core.Managers;
using CreditWorks.VehicleManagement.Vehicles.Models;
using CreditWorks.VehicleManagement.Vehicles.VehicleEditing.Actions.Update;
using CreditWorks.VehicleManagement.Vehicles.VehicleListing;
using CreditWorks.VehicleManagement.Vehicles.VehicleListing.Actions;
using CreditWorks.VehicleManagement.Vehicles.VehicleListing.Actions.List;
using Fluxor;
using System.Collections.Immutable;

namespace CreditWorks.VehicleManagement.Vehicles.VehicleEditing.Actions.Save
{
    public class SaveEffect : Effect<SaveAction>
    {
        private readonly VehicleManager _manager;
        private readonly ILogger<SaveEffect> _logger;
        private readonly IState<VehicleState> _state;
        private readonly IState<VehicleListState> _listState;

        public SaveEffect(VehicleManager manager, ILogger<SaveEffect> logger, IState<VehicleState> state, IState<VehicleListState> listState)
        {
            _manager = manager;
            _logger = logger;
            _state = state;
            _listState = listState;
        }

        public override async Task HandleAsync(SaveAction action, IDispatcher dispatcher)
        {
            try
            {
                if (_state.Value.UnderEdit != null)
                {
                    var category = _listState.Value.Categories.Single(c => c.Id == _state.Value.UnderEdit.Category).Id;
                    var manufacturer = _listState.Value.Manufacturers.Single(c => c.Id == _state.Value.UnderEdit.Manufacturer).Id;

                    var dbVehicle = GenerateDBVehicle(_state.Value.UnderEdit!);

                    if (_state.Value.UnderEdit.Id != "NEW")
                        await _manager.UpdateVehicle(dbVehicle);
                    else
                        await _manager.CreateVehicle(dbVehicle);

                    dispatcher.Dispatch(new SaveSuccessAction());
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error setting category minimum weight, reason: {ex.Message}";
                _logger.LogError("{Message}", errorMessage);
                dispatcher.Dispatch(new VehicleFailureAction(ex.Message));
            }
        }

        private static Data.Models.Vehicle GenerateDBVehicle(Vehicle vehicle)
        {
            var dbVehicle = new Data.Models.Vehicle();

            //for new categories (Id < 0), new Id is generated by the DB
            //But we need to set it for existing ones
            if (vehicle.Id != "NEW")
            {
                var id = Convert.ToInt32(vehicle.Id);
                if (id > 0)
                    dbVehicle.Id = id;
            }

            dbVehicle.Owner = vehicle.Owner!;
            dbVehicle.ManufacturerId = vehicle.Manufacturer!.Value;
            dbVehicle.CategoryId = vehicle.Category!.Value;
            dbVehicle.Year = vehicle.Year!.Value;
            dbVehicle.Weight = vehicle.Weight!.Value;

            return dbVehicle;
        }
    }
}
