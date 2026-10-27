import asyncio, os, json, sys
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
root=Path(__file__).resolve().parents[1]/'unity-mcp'/'Server'
async def main():
    env=dict(os.environ,UNITY_MCP_DISABLE_TELEMETRY='true',PYTHONUTF8='1')
    params=StdioServerParameters(command=str(root/'.venv'/'Scripts'/'python.exe'),args=[str(root/'src'/'main.py'),'--transport','stdio','--default-instance','UnityProject'],env=env)
    async with stdio_client(params) as (r,w):
        async with ClientSession(r,w) as session:
            init=await session.initialize()
            tools=await session.list_tools()
            resources=await session.list_resources()
            result={'server':init.serverInfo.model_dump(),'tools':len(tools.tools),'resources':[str(x.uri) for x in resources.resources]}
            for res in resources.resources:
                if 'instances' in str(res.uri):
                    data=await session.read_resource(res.uri)
                    result['instances']=[getattr(c,'text','') for c in data.contents]
            if "--verify-scene" in sys.argv:
                if "--load-scene" in sys.argv:
                    result["loadScene"]=(await session.call_tool("manage_scene", {"action":"load","path":"Assets/Scenes/Qingming.unity"})).model_dump(mode="json")
                result["activeScene"]=(await session.call_tool("manage_scene", {"action":"get_active"})).model_dump(mode="json")
                result["hierarchy"]=(await session.call_tool("manage_scene", {"action":"get_hierarchy","page_size":5,"max_nodes":5})).model_dump(mode="json")
            Path(__file__).with_name("McpConnectionCheck.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
            print(json.dumps(result,ensure_ascii=False,indent=2))
asyncio.run(main())
