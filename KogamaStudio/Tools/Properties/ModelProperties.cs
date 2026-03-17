using Assets.Scripts.WorldObjectTypes.EditablePickupItem;
using MV.WorldObject;
using WorldObjectTypes.MVDoor;
using WorldObjectTypes.VehicleEnergy;
using KogamaModFramework.Operations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KogamaStudio.Tools.Properties;

internal class ModelProperties
{
    internal static MVCubeModelBase CubeModelBase;

    internal static MVCubeModelBase GetModel(MVWorldObjectClient mVWorldObjectClient) => GetModelFromWorldObject(mVWorldObjectClient);

    private static MVCubeModelBase GetModelFromWorldObject(MVWorldObjectClient mVWorldObjectClient)
    {
        if (mVWorldObjectClient == null) return null;

        MVCubeModelBase result = null;

        try
        {
            var spawnerVehicle = mVWorldObjectClient.Cast<MVWorldObjectSpawnerVehicle>();
            var spawnWorldObject = MVGameControllerBase.WOCM.GetWorldObjectClient(spawnerVehicle.SpawnWorldObjectID);
            result = GetModelFromWorldObject(spawnWorldObject);
            return result;
        }
        catch { }

        try { result = mVWorldObjectClient.Cast<MVSentryGunBlueprint>().EditableCubesWO; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVMovingPlatformGroup>().Platform.CubeModel; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVRotator>().CubeModel; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<CollectTheItemCollectable>().editableCubeModelWrapper.CubeModel; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<CollectTheItemDropOff>().editableCubeModelWrapper.CubeModel; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVAdvancedGhost>().editableCubeModelWrapper.CubeModel; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVJetPack>().editableCubeModelWrapper.CubeModel; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVHoverCraft>().editableCubeModelWrapper.CubeModel; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVEditablePickupItemBaseBlueprint>().editableCubeModel; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVWorldObjectSpawnerVehicleEnergy>().vehicleEnergyChild.CubeModelInstance; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVDoorBlueprint>().DoorLogic.doorModelInstance; return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVCubeModelInstance>(); return result; } catch { }
        try { result = mVWorldObjectClient.Cast<MVCubeModelPrototypeTerrain>(); return result; } catch { }

        return null;
    }

}