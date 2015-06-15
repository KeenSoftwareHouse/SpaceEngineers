using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
   public class SequenceGroup : Sequence
    {
       private readonly ISequencer sequencer;

       public SequenceGroup(ISequencer sequencer) : base(-1)
       {
           this.sequencer = sequencer;
       }

    }
}
