# Development
- Download and install Visual Studio
- Fork this repository and clone it
- Reference the required dlls by adding them to `AssembleProjection.csproj.user`  
  E.g.  

  ```
  <?xml version="1.0" encoding="utf-8"?>
  <Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup>
    <Reference Include="Sandbox.Common">
      <HintPath>%STEAM LIBRARY PATH HERE%\steamapps\common\SpaceEngineers\Bin64\Sandbox.Common.dll</HintPath>
    </Reference>
    <Reference Include="Sandbox.Game">
      <HintPath>%STEAM LIBRARY PATH HERE%\steamapps\common\SpaceEngineers\Bin64\Sandbox.Game.dll</HintPath>
    </Reference>
    <Reference Include="SpaceEngineers.Game">
      <HintPath>%STEAM LIBRARY PATH HERE%\steamapps\common\SpaceEngineers\Bin64\SpaceEngineers.Game.dll</HintPath>
    </Reference>
    <Reference Include="SpaceEngineers.ObjectBuilders">
      <HintPath>%STEAM LIBRARY PATH HERE%\steamapps\common\SpaceEngineers\Bin64\SpaceEngineers.ObjectBuilders.dll</HintPath>
    </Reference>
    <Reference Include="VRage">
      <HintPath>%STEAM LIBRARY PATH HERE%\steamapps\common\SpaceEngineers\Bin64\VRage.dll</HintPath>
    </Reference>
    <Reference Include="VRage.Game">
      <HintPath>%STEAM LIBRARY PATH HERE%\steamapps\common\SpaceEngineers\Bin64\VRage.Game.dll</HintPath>
    </Reference>
    <Reference Include="VRage.Library">
      <HintPath>%STEAM LIBRARY PATH HERE%\steamapps\common\SpaceEngineers\Bin64\VRage.Library.dll</HintPath>
    </Reference>
    <Reference Include="VRage.Math">
      <HintPath>%STEAM LIBRARY PATH HERE%\steamapps\common\SpaceEngineers\Bin64\VRage.Math.dll</HintPath>
    </Reference>
  </ItemGroup>
  </Project>
  ```
