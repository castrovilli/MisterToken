﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MisterToken {
    public interface SinglePlayerListener {
        void OnClear(PlayerIndex player);
        void OnWon(PlayerIndex player);
        void OnFailed(PlayerIndex player);
        void OnFinished(PlayerIndex player);
    }
}
