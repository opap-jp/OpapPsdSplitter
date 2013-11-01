﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop PSD FileType Plugin for Paint.NET
// http://psdplugin.codeplex.com/
//
// This software is provided under the MIT License:
//   Copyright (c) 2006-2007 Frank Blumenberg
//   Copyright (c) 2010-2012 Tao Yue
//
// Portions of this file are provided under the BSD 3-clause License:
//   Copyright (c) 2006, Jonas Beckeman
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Globalization;

namespace PhotoshopFile
{
  public class BlendingRanges
  {
    /// <summary>
    /// The layer to which this channel belongs
    /// </summary>
    public Layer Layer { get; private set; }

    public byte[] Data { get; set; }

    ///////////////////////////////////////////////////////////////////////////

    public BlendingRanges(Layer layer)
    {
      Layer = layer;
      Data = new byte[0];
    }

    ///////////////////////////////////////////////////////////////////////////

    public BlendingRanges(PsdBinaryReader reader, Layer layer)
    {
      Debug.WriteLine("BlendingRanges started at " + reader.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

      Layer = layer;
      var dataLength = reader.ReadInt32();
      if (dataLength <= 0)
      {
          Data = new byte[0]; // modified to avoid exception at Save method (butaman)
          return;
      }
      Data = reader.ReadBytes(dataLength);
    }

    ///////////////////////////////////////////////////////////////////////////

    public void Save(PsdBinaryWriter writer)
    {
      Debug.WriteLine("BlendingRanges Save started at " + writer.BaseStream.Position.ToString(CultureInfo.InvariantCulture));

      writer.Write((UInt32)Data.Length);
      writer.Write(Data);
    }
  }
}
