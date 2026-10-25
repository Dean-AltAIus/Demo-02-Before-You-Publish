"""Versioned entry point for the portable, non-overwriting reviewer exporter."""
import runpy
from pathlib import Path
runpy.run_path(str(Path(__file__).with_name("reexport_module_lead.py")),run_name="__main__")
