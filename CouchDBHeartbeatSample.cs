/*
*
*    CouchDBHeartbeat - CouchDB changes API for Silverlight (Chunked HTTP response parser in C#).
*    Copyright (C) 2010 BLITZ Digital Studios LLC d/b/a BLITZ Agency.
*
*    This library is free software; you can redistribute it and/or modify it 
*    under the terms of the GNU Lesser General Public License as published
*    by the Free Software Foundation; either version 2.1 of the License, or 
*    (at your option) any later version.
*
*    This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
*    without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
*    PURPOSE. See the GNU Lesser General Public License for more details.
*
*    You should have received a copy of the GNU Lesser General Public License along 
*    with this library; if not, write to the Free Software Foundation, Inc.,
*    59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
*
*    BLTIZ Digital Studios LLC, 1453 3rd Street Promenade, Ste 420, Santa Monica CA, 90401
*    http://www.blitzagency.com
*    http://labs.blitzagency.com
*
*        Author: Adam Venturella - aventurella@blitzagency.com
*
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using BlitzAgency.Net.CouchDB;
using System.Diagnostics;

namespace Sample
{
	public class CouchDBHeartbeatSample
	{
		CouchDBHeartbeat Heartbeat;
		
		public function Sample()
		{
			Heartbeat = new CouchDBHeartbeat();
			Heartbeat.CouchDBHeartbeatUpdate += new CouchDBHeartbeat.CouchDBHeartbeatHandler(Heartbeat_CouchDBHeartbeatUpdate);
			Heartbeat.Begin();
		}
		
		private void Heartbeat_CouchDBHeartbeatUpdate(object sender, CouchDBHeartbeatEventArgs e)
        { 
            
            Debug.WriteLine(e.Document.ToString());
            
            // In silverlight if you would like to get this data back to the UI thread in Silverlight:
           	/*
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(delegate 
            {
            	// Talk to the UI thread here
            });
            */           
        }
	}
}