"""Send an explicit tool request to the configured local Unity MCP server."""
import asyncio
import json
import os
import sys
from pathlib import Path
from datetime import timedelta
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def main():
    root=Path(__file__).resolve().parents[1]/"unity-mcp"/"Server"
    params=StdioServerParameters(command=str(root/".venv/Scripts/python.exe"),
        args=[str(root/"src/main.py"),"--transport","stdio","--default-instance","UnityProject"],
        env=dict(os.environ,UNITY_MCP_DISABLE_TELEMETRY="true",PYTHONUTF8="1"))
    request=json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
    async with stdio_client(params) as (r,w):
        async with ClientSession(r,w,read_timeout_seconds=timedelta(seconds=180)) as session:
            await session.initialize()
            result=await session.call_tool(request["tool"],request["arguments"])
            payload=result.model_dump(mode="json")
            Path(sys.argv[1]).with_suffix(".result.json").write_text(json.dumps(payload,ensure_ascii=False,indent=2),encoding="utf-8")
            print(json.dumps(payload,ensure_ascii=False,indent=2))
asyncio.run(main())
