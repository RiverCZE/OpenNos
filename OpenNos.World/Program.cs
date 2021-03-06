﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using log4net;
using OpenNos.Core;
using OpenNos.Core.Networking.Communication.Scs.Communication.EndPoints.Tcp;
using OpenNos.DAL;
using OpenNos.DAL.EF.Helpers;
using OpenNos.Data;
using OpenNos.GameObject;
using OpenNos.Handler;
using OpenNos.WebApi.Reference;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace OpenNos.World
{
    public class Program
    {
        #region Members

        private const string IPADDRESS = "127.0.0.1";

        private static EventHandler exitHandler;
        private static ManualResetEvent run = new ManualResetEvent(true);

        #endregion

        #region Delegates

        private delegate bool EventHandler(CtrlType sig);

        #endregion

        #region Enums

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        #endregion

        #region Methods

        public static void Main(string[] args)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

            // initialize Logger
            Logger.InitializeLogger(LogManager.GetLogger(typeof(Program)));
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            Console.Title = $"OpenNos World Server v{fileVersionInfo.ProductVersion}";
            int port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["WorldPort"]);
            string text = $"WORLD SERVER v{fileVersionInfo.ProductVersion} - by OpenNos Team";
            int offset = Console.WindowWidth / 2 + text.Length / 2;
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}", text) + "\n" + separator);

            // initialize api
            ServerCommunicationClient.Instance.InitializeAndRegisterCallbacks();

            // initialize DB
            if (DataAccessHelper.Initialize())
            {
                // register mappings for DAOs, Entity -> GameObject and GameObject -> Entity
                RegisterMappings();

                // initialilize maps
                ServerManager.Instance.Initialize(IPADDRESS, port);
            }
            else
            {
                Console.ReadLine();
                return;
            }

            // TODO: initialize ClientLinkManager initialize PacketSerialization
            PacketFactory.Initialize<WalkPacket>();

            try
            {
                exitHandler += ExitHandler;
                SetConsoleCtrlHandler(exitHandler, true);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("General Error", ex);
            }
            NetworkManager<WorldEncryption> networkManager = null;
            portloop:
            try
            {
                networkManager = new NetworkManager<WorldEncryption>(IPADDRESS, port, typeof(CommandPacketHandler), typeof(LoginEncryption), true);
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                if(ex.ErrorCode == 10048)
                {
                    port++;
                    Logger.Log.Info("Port already in use! Incrementing...");
                    goto portloop;
                }
                else
                {
                    Logger.Log.Error("General Error", ex);
                    Environment.Exit(1);
                }
            }

            string serverGroup = System.Configuration.ConfigurationManager.AppSettings["ServerGroup"];
            int sessionLimit = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SessionLimit"]);
            int? newChannelId = ServerCommunicationClient.Instance.HubProxy.Invoke<int?>("RegisterWorldserver", serverGroup, new WorldserverDTO(ServerManager.Instance.WorldId, new ScsTcpEndPoint(IPADDRESS, port), sessionLimit)).Result;

            if (newChannelId.HasValue)
            {
                ServerManager.Instance.ChannelId = newChannelId.Value;
            }
            else
            {
                Logger.Log.ErrorFormat("Could not retrieve ChannelId from Web API.");
            }
        }

        private static bool ExitHandler(CtrlType sig)
        {
            string serverGroup = System.Configuration.ConfigurationManager.AppSettings["ServerGroup"];
            int port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["WorldPort"]);
            ServerCommunicationClient.Instance.HubProxy.Invoke("UnregisterWorldserver", serverGroup, new ScsTcpEndPoint(IPADDRESS, port)).Wait();

            ServerManager.Instance.Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_SEC"), 5));
            ServerManager.Instance.SaveAll();

            Thread.Sleep(5000);
            return false;
        }

        private static void RegisterMappings()
        {
            // register mappings for items
            DAOFactory.IteminstanceDao.RegisterMapping(typeof(BoxInstance));
            DAOFactory.IteminstanceDao.RegisterMapping(typeof(SpecialistInstance));
            DAOFactory.IteminstanceDao.RegisterMapping(typeof(WearableInstance));
            DAOFactory.IteminstanceDao.InitializeMapper(typeof(ItemInstance));

            // entities
            DAOFactory.AccountDAO.RegisterMapping(typeof(Account)).InitializeMapper();
            DAOFactory.CellonOptionDAO.RegisterMapping(typeof(CellonOptionDTO)).InitializeMapper();
            DAOFactory.CharacterDAO.RegisterMapping(typeof(Character)).InitializeMapper();
            DAOFactory.CharacterRelationDAO.RegisterMapping(typeof(CharacterRelationDTO)).InitializeMapper();
            DAOFactory.CharacterSkillDAO.RegisterMapping(typeof(CharacterSkill)).InitializeMapper();
            DAOFactory.ComboDAO.RegisterMapping(typeof(ComboDTO)).InitializeMapper();
            DAOFactory.DropDAO.RegisterMapping(typeof(DropDTO)).InitializeMapper();
            DAOFactory.GeneralLogDAO.RegisterMapping(typeof(GeneralLogDTO)).InitializeMapper();
            DAOFactory.ItemDAO.RegisterMapping(typeof(ItemDTO)).InitializeMapper();
            DAOFactory.MailDAO.RegisterMapping(typeof(MailDTO)).InitializeMapper();
            DAOFactory.MapDAO.RegisterMapping(typeof(MapDTO)).InitializeMapper();
            DAOFactory.MapMonsterDAO.RegisterMapping(typeof(MapMonster)).InitializeMapper();
            DAOFactory.MapNpcDAO.RegisterMapping(typeof(MapNpc)).InitializeMapper();
            DAOFactory.FamilyDAO.RegisterMapping(typeof(FamilyDTO)).InitializeMapper();
            DAOFactory.FamilyCharacterDAO.RegisterMapping(typeof(FamilyCharacterDTO)).InitializeMapper();
            DAOFactory.FamilyLogDAO.RegisterMapping(typeof(FamilyLogDTO)).InitializeMapper();
            DAOFactory.MapTypeDAO.RegisterMapping(typeof(MapTypeDTO)).InitializeMapper();
            DAOFactory.MapTypeMapDAO.RegisterMapping(typeof(MapTypeMapDTO)).InitializeMapper();
            DAOFactory.NpcMonsterDAO.RegisterMapping(typeof(NpcMonster)).InitializeMapper();
            DAOFactory.NpcMonsterSkillDAO.RegisterMapping(typeof(NpcMonsterSkill)).InitializeMapper();
            DAOFactory.PenaltyLogDAO.RegisterMapping(typeof(PenaltyLogDTO)).InitializeMapper();
            DAOFactory.PortalDAO.RegisterMapping(typeof(PortalDTO)).InitializeMapper();
            DAOFactory.QuicklistEntryDAO.RegisterMapping(typeof(QuicklistEntryDTO)).InitializeMapper();
            DAOFactory.RecipeDAO.RegisterMapping(typeof(Recipe)).InitializeMapper();
            DAOFactory.RecipeItemDAO.RegisterMapping(typeof(RecipeItemDTO)).InitializeMapper();
            DAOFactory.RespawnDAO.RegisterMapping(typeof(RespawnDTO)).InitializeMapper();
            DAOFactory.RespawnMapTypeDAO.RegisterMapping(typeof(RespawnMapTypeDTO)).InitializeMapper();
            DAOFactory.ShopDAO.RegisterMapping(typeof(Shop)).InitializeMapper();
            DAOFactory.ShopItemDAO.RegisterMapping(typeof(ShopItemDTO)).InitializeMapper();
            DAOFactory.ShopSkillDAO.RegisterMapping(typeof(ShopSkillDTO)).InitializeMapper();
            DAOFactory.SkillDAO.RegisterMapping(typeof(Skill)).InitializeMapper();
            DAOFactory.TeleporterDAO.RegisterMapping(typeof(TeleporterDTO)).InitializeMapper();
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        #endregion
    }
}