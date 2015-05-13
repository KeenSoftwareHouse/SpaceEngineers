using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyGamePaths
    {
       string ContentPath {get;} 
       string ModsPath {get;} 
       string UserDataPath {get;}
       string SavesPath { get; }
    }
}
