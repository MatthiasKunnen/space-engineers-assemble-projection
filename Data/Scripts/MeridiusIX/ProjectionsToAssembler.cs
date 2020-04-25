using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace MeridiusIX{
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	
	public class ProjectionsToAssembler : MySessionComponentBase{
		
		Dictionary<string, string> blueprintDictionary = new Dictionary<string, string>(); //Component Name (SubtypeId) & Blueprint Name (SubtypeId)
		bool scriptInitialized = false;
		Guid storageKey = new Guid("1C45E98F-30A7-41BF-A15B-ECC475302BFE");
		
		public override void UpdateBeforeSimulation(){
			
			if(scriptInitialized == false/* && MyAPIGateway.Multiplayer.IsServer == true*/){
				
				//create dummy projector/pb
				
				var randomDir = MyUtils.GetRandomVector3D();
				var randomSpawn = randomDir * 1000000;
				var prefab = MyDefinitionManager.Static.GetPrefabDefinition("Dummy PB-Proj");
				var gridOB = prefab.CubeGrids[0];
				gridOB.PositionAndOrientation = new MyPositionAndOrientation(randomSpawn, Vector3.Forward, Vector3.Up);
				MyAPIGateway.Entities.RemapObjectBuilder(gridOB);
				var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridOB);
				
				
				var assemblerList = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyProjector>("AssemblerList");
				assemblerList.Title = MyStringId.GetOrCompute("Assembler List");
				assemblerList.Multiselect = false;
				assemblerList.VisibleRowsCount = 5;
				assemblerList.ListContent = AssemblerListCreate;
				assemblerList.ItemSelected = AssemblerListSelect;
				MyAPIGateway.TerminalControls.AddControl<IMyProjector>(assemblerList);
				
				var projectorButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("SendGridToAssembler");
				projectorButton.Title = MyStringId.GetOrCompute("Send To Assembler");
				projectorButton.Action = ProjectorAction;
				MyAPIGateway.TerminalControls.AddControl<IMyProjector>(projectorButton);
				
				var projectorButtonB = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("SendMissingToAssembler");
				projectorButtonB.Title = MyStringId.GetOrCompute("Send Missing Items");
				projectorButtonB.Action = ProjectorActionB;
				MyAPIGateway.TerminalControls.AddControl<IMyProjector>(projectorButtonB);
				
				List<MyBlueprintDefinitionBase> blueprintList = MyDefinitionManager.Static.GetBlueprintDefinitions().Where(x => x.Results[0].Id.TypeId != typeof(MyObjectBuilder_Ore)).ToList();
				
				foreach(var blueprint in blueprintList){
					
					if(blueprint.Results[0].Id.TypeId.ToString().Contains("MyObjectBuilder_Component") == true){
						
						if(blueprintDictionary.ContainsKey(blueprint.Results[0].Id.SubtypeId.ToString()) == false){
							
							blueprintDictionary.Add(blueprint.Results[0].Id.SubtypeId.ToString(), blueprint.Id.SubtypeId.ToString());
							
						}
						
					}
						
				}
				
				entity.Delete();
				scriptInitialized = true;
							
			}
			
		}
		
		void AssemblerListCreate(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> itemList, List<MyTerminalControlListBoxItem> selectedItems){
			
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(block.SlimBlock.CubeGrid);
			var assemblerList = new List<IMyAssembler>();
			gts.GetBlocksOfType<IMyAssembler>(assemblerList);
			string selectedAssemblerId = "";
			
			if(block.Storage == null){
				
				block.Storage = new MyModStorageComponent();
				
			}else{
				
				if(block.Storage.ContainsKey(storageKey) == true){
					
					selectedAssemblerId = block.Storage[storageKey];
					
				}else{
					
					block.Storage.Add(storageKey, "");
					
				}
				
			}
			
			bool foundSelected = false;
			
			if(assemblerList.Count == 0){
				
				block.Storage[storageKey] = "";
				return;
				
			}
			
			foreach(var assembler in assemblerList){
				
				var assemblerId = assembler.EntityId.ToString();
				var listItem = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(assembler.CustomName), MyStringId.GetOrCompute(""), assemblerId);
				itemList.Add(listItem);
				
				if(assemblerId == selectedAssemblerId && foundSelected == false){
					
					foundSelected = true;
					selectedItems.Add(listItem);
					
				}
				
			}
			
			if(selectedItems.Count == 0 && itemList.Count > 0 && foundSelected == false){
				
				selectedItems.Add(itemList[0]);
				var idName = itemList[0].UserData as string;
				block.Storage[storageKey] = idName;
				
			}
			
		}
		
		void AssemblerListSelect(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> selectedItems){
			
			if(selectedItems.Count == 0){
				
				return;
				
			}
			
			var idName = selectedItems[0].UserData as string;
			block.Storage[storageKey] = idName;
			
		}
		
		void ProjectorAction(IMyTerminalBlock block){
			
			ProcessProjection(block, true);
			
		}
		
		void ProjectorActionB(IMyTerminalBlock block){
			
			ProcessProjection(block, false);
			
		}
		
		void ProcessProjection(IMyTerminalBlock block, bool allBlocks){
			
			var projector = block as IMyProjector;
			
			if(projector == null || block.IsWorking == false){
				
				return;
				
			}
			
			if(projector.ProjectedGrid == null){
				
				return;
				
			}
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)block.CubeGrid;
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			IMyAssembler primaryAssembler = null;
			var assemblerList = new List<IMyAssembler>();
			gts.GetBlocksOfType<IMyAssembler>(assemblerList);
			
			for(int i = assemblerList.Count - 1; i >= 0; i--){
				
				var assembler = assemblerList[i];
				string outputValue = "";
				block.Storage.TryGetValue(storageKey, out outputValue);
				
				if(assembler.IsWorking == false || assembler.IsFunctional == false || assembler.Mode == Sandbox.ModAPI.Ingame.MyAssemblerMode.Disassembly){
					
					if(outputValue == assembler.EntityId.ToString()){
						
						return; //Selected Assember is Not Working
						
					}

					continue;
					
				}
				
				if(outputValue == assembler.EntityId.ToString()){
					
					primaryAssembler = assembler;
					break;
					
				}
				
			}
			
			if(primaryAssembler == null){
				
				return;
				
			}
			
			var queueDictionary = GetBlocksForQueue(projector, allBlocks);
			
			if(allBlocks == false){
				
				var existingParts = GetExistingParts(primaryAssembler);
				
				foreach(var component in existingParts.Keys){
					
					if(queueDictionary.ContainsKey(component) == true){
						
						if(existingParts[component] >= queueDictionary[component]){
							
							queueDictionary[component] = 0;
							
						}else{
							
							queueDictionary[component] -= existingParts[component];
							
						}
						
					}
					
				}
				
			}
			
			foreach(var component in queueDictionary.Keys){
				
				if(blueprintDictionary.ContainsKey(component) == true){
					
					MyDefinitionId blueprint = new MyDefinitionId();
	
					if(MyDefinitionId.TryParse("MyObjectBuilder_BlueprintDefinition/" + blueprintDictionary[component], out blueprint) == true){
						
						if(primaryAssembler.CanUseBlueprint(blueprint) == true){
							
							primaryAssembler.AddQueueItem(blueprint, (MyFixedPoint)queueDictionary[component]);
							
						}
						
					}
					
				}
				
			}
			
		}
		
		Dictionary<string, int> GetBlocksForQueue(IMyProjector projector, bool allBlocks){
			
			var resultDictionary = new Dictionary<string, int>();
			var projectedGrid = projector.ProjectedGrid;
			var blockList = new List<IMySlimBlock>();
			projectedGrid.GetBlocks(blockList);
			
			foreach(var block in blockList){
				
				var blockDefininition = block.BlockDefinition as MyCubeBlockDefinition;
				var blockcomponents = blockDefininition.Components;
				
				if(allBlocks == false){
					
					if(projector.CanBuild(block, true) == BuildCheckResult.AlreadyBuilt){
						
						continue;
						
					}
					
				}
				
				foreach(var component in blockcomponents){
					
					if(resultDictionary.ContainsKey(component.Definition.Id.SubtypeId.ToString()) == true){
						
						resultDictionary[component.Definition.Id.SubtypeId.ToString()] += component.Count;
						
					}else{
						
						resultDictionary.Add(component.Definition.Id.SubtypeId.ToString(), component.Count);
						
					}
					
				}
				
			}
			
			return resultDictionary;
			
		}
		
		Dictionary<string, int> GetExistingParts(IMyAssembler assembler){
			
			var resultDict = new Dictionary<string, int>();
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)assembler.CubeGrid;
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			var blockList = new List<IMyTerminalBlock>();
			gts.GetBlocksOfType<IMyTerminalBlock>(blockList);
			
			foreach(var block in blockList){
				
				if(block.HasInventory == false){
					
					continue;
					
				}
				
				var blockInv = block.GetInventory(0);
				var blockItems = blockInv.GetItems();
				
				foreach(var item in blockItems){
					
					if(item.Content.TypeId.ToString().Contains("Component") == true){
						
						var subtype = item.Content.SubtypeId.ToString();
						var amount = (int)item.Amount;
						
						if(resultDict.ContainsKey(subtype) == true){
							
							resultDict[subtype] += amount;
							
						}else{
							
							resultDict.Add(subtype, amount);
							
						}
						
					}
					
				}
				
				if(block.InventoryCount > 1){
					
					blockInv = block.GetInventory(1);
					blockItems = blockInv.GetItems();
					
					foreach(var item in blockItems){
						
						if(item.Content.TypeId.ToString().Contains("Component") == true){
							
							var subtype = item.Content.SubtypeId.ToString();
							var amount = (int)item.Amount;
							
							if(resultDict.ContainsKey(subtype) == true){
								
								resultDict[subtype] += amount;
								
							}else{
								
								resultDict.Add(subtype, amount);
								
							}
							
						}
						
					}
					
				}
				
			}
			
			return resultDict;
			
		}
		
	}
	
}