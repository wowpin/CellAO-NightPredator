﻿#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using System.Collections.Generic;
    using System.Linq;

    using CellAO.Communication.Messages;
    using CellAO.Core.Components;
    using CellAO.Core.Entities;
    using CellAO.Core.Network;
    using CellAO.Core.Playfields;

    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// </summary>
    [MessageHandler(MessageHandlerDirection.InboundOnly)]
    public class VicinityChatMessageHandler : BaseMessageHandler<TextMessage, VicinityChatMessageHandler>
    {
        #region Inbound

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        /// <param name="client">
        /// </param>
        protected override void Read(TextMessage message, IZoneClient client)
        {
            if (message.Message.Text.StartsWith("."))
            {
                MessageWrapper<ChatCmdMessage> wrapper = new MessageWrapper<ChatCmdMessage>()
                                                         {
                                                             Client = client,
                                                             Message = null,
                                                             MessageBody =
                                                                 new ChatCmdMessage()
                                                                 {
                                                                     Command
                                                                         =
                                                                         message
                                                                         .Message
                                                                         .Text
                                                                         .TrimStart
                                                                         (
                                                                             '.'),
                                                                     Identity
                                                                         =
                                                                         client
                                                                         .Controller
                                                                         .Character
                                                                         .Identity,
                                                                     Target
                                                                         =
                                                                         client
                                                                         .Controller
                                                                         .Character
                                                                         .SelectedTarget
                                                                 }
                                                         };

                // It is a chat command in vicinity chat, lets process it
                ChatCmdMessageHandler.Default.Receive(wrapper); // manually call the receive()
            }
            else
            {
                ICharacter character = client.Controller.Character;
                IPlayfield playfield = character.Playfield;

                float range = 0.0f;
                switch ((int)message.Message.Type)
                {
                    case 0x01:
                        range = 1.5f;
                        break;
                    case 0x00:
                        range = 10.0f;
                        break;
                    case 0x02:
                        range = 60.0f;
                        break;
                }

                List<IDynel> charsInRange = playfield.FindInRange(character, range);

                VicinityChatMessage vicinityChat = new VicinityChatMessage
                                                   {
                                                       CharacterIds =
                                                           charsInRange.Select(
                                                               x => x.Identity.Instance)
                                                           .ToList(),
                                                       MessageType = (byte)message.Message.Type,
                                                       Text = message.Message.Text,
                                                       SenderId = character.Identity.Instance
                                                   };

                Program.ISComClient.Send(vicinityChat);
            }
        }

        #endregion
    }
}