using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[assembly: ModInfo("Water Weather Simulation", "waterweathersimulation",
	Authors = new [] {"WarlikeCracker"},
	Description = "Melts ice at +4 and freeze water to ice at -4 during chunk load",
	Version = "1.0.0",
	Side = "Server",
	RequiredOnClient = false)]

namespace WaterWeatherSimulation;

public class WaterWeatherSimulationModSystem : ModSystem
{
	private IWorldAccessor world;
	private IBulkBlockAccessor bba;

	private int iceBlockId;
	private int waterBlockId;
	
	public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;
	
	public override void StartServerSide(ICoreServerAPI api)
	{
		var iceBlock = api.World.GetBlock(AssetLocation.Create("lakeice"));
		var waterBlock = api.World.GetBlock(AssetLocation.Create("water-still-7"));
		if (iceBlock == null || waterBlock == null) return;
		
		iceBlockId = iceBlock.BlockId;
		waterBlockId = waterBlock.BlockId;
		world = api.World;
		bba = api.World.GetBlockAccessorMapChunkLoading(false);
		api.Event.BeginChunkColumnLoadChunkThread += EventOnBeginChunkColumnLoadChunkThread;
	}

	private void EventOnBeginChunkColumnLoadChunkThread(IServerMapChunk mapChunk, int chunkX, int chunkZ, IWorldChunk[] chunks)
	{
		bba.SetChunks(new Vec2i(chunkX, chunkZ), chunks);
		
		var blockPos = new BlockPos(0, Climate.Sealevel - 1, 0, 0);
		for (var x = chunkX * 32; x < chunkX * 32 + 32; x += 1 )
			for (var z = chunkZ * 32; z < chunkZ * 32 + 32; z += 1)
			{
				blockPos.X = x;
				blockPos.Z = z;
				
				// Rain map height may return value out of world.
				var y = bba.GetRainMapHeightAt(blockPos);
				blockPos.Y = y < bba.MapSizeY ? y : Climate.Sealevel - 1;
			
				var block = bba.GetBlock(blockPos, BlockLayersAccess.Fluid);
				if (block.BlockId != waterBlockId && block.BlockId != iceBlockId) continue;
			
				var temp = bba.GetClimateAt(blockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, world.Calendar.TotalDays).Temperature;
				if (temp < 4f)
					bba.SetBlock(iceBlockId, blockPos, BlockLayersAccess.Fluid);
				else if (temp > 4f)
					bba.SetBlock(waterBlockId, blockPos, BlockLayersAccess.Fluid);
			}
	
		bba.Commit();
	}
}

