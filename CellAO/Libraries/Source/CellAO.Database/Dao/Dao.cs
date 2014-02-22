﻿#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
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

#endregion

namespace CellAO.Database.Dao
{
    #region Usings ...

    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;

    using CellAO.Core.Exceptions;
    using CellAO.Database.Entities;

    using Dapper;

    #endregion

    /// <summary>
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public class Dao<T> : IDao<T>
        where T : IDBEntity, new()
    {
        #region Static Fields

        /// <summary>
        /// </summary>
        private static Dictionary<string, PropertyInfo> cachedProperties = null;

        /// <summary>
        /// </summary>
        private static Dao<T> instance = default(Dao<T>);

        #endregion

        #region Public Properties

        /// <summary>
        /// </summary>
        public static Dao<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Dao<T>();
                    if (typeof(T).GetCustomAttributes(typeof(TablenameAttribute), false).Any())
                    {
                        instance.TableName =
                            ((TablenameAttribute)typeof(T).GetCustomAttributes(typeof(TablenameAttribute), false).First())
                                .Tablename;
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// </summary>
        public string TableName { get; set; }

        #endregion

        #region Properties

        /// <summary>
        /// Cache the properties for fast access
        /// </summary>
        protected static Dictionary<string, PropertyInfo> CachedProperties
        {
            get
            {
                if (cachedProperties == null)
                {
                    cachedProperties=new Dictionary<string, PropertyInfo>();
                    lock (cachedProperties)
                    {
                        cachedProperties.Clear();
                        foreach (PropertyInfo property in typeof(T).GetProperties())
                        {
                            cachedProperties.Add(property.Name, property);
                        }
                    }
                }

                return cachedProperties;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="entity">
        /// </param>
        /// <param name="transaction">
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="DataBaseException">
        /// </exception>
        public int Add(T entity, IDbTransaction transaction = null)
        {
            int rowsAffected = 0;

            using (IDbConnection conn = Connector.GetConnection())
            {
                rowsAffected = conn.Execute(
                    SqlMapperUtil.CreateInsertSQL(this.TableName, this.getAllParameters()),
                    entity,
                    transaction);

                // we must retrive the Id anyway here. we need to standardise the id as we started to do.
                if (rowsAffected == 1)
                {
                    SqlMapperUtil.SetIdentity<int>(conn, id => entity.Id = id, transaction);
                }
                else
                {
                    throw new DataBaseException(
                        string.Format("Failed to create new record on table '{0}'", this.TableName));
                }
            }

            return rowsAffected;
        }

        /// <summary>
        /// Delete a row from the table
        /// </summary>
        /// <param name="entityId">
        /// id of the row to delete
        /// </param>
        /// <param name="transaction">
        /// optional transaction for the delete
        /// </param>
        public void Delete(int entityId, IDbTransaction transaction = null)
        {
            using (IDbConnection conn = Connector.GetConnection())
            {
                conn.Execute(SqlMapperUtil.CreateDeleteSQL(this.TableName), new { id = entityId }, transaction);
            }
        }

        /// <summary>
        /// Delete rows from table
        /// </summary>
        /// <param name="whereParameters">
        /// optional DynamicParameters with the items for the where-clause
        /// </param>
        /// <param name="transaction">
        /// optional transaction for the delete
        /// </param>
        public void Delete(DynamicParameters whereParameters, IDbTransaction transaction = null)
        {
            using (IDbConnection conn = Connector.GetConnection())
            {
                conn.Execute(SqlMapperUtil.CreateDeleteSQL(this.TableName, whereParameters), transaction: transaction);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="entityId">
        /// </param>
        /// <returns>
        /// </returns>
        public bool Exists(int entityId)
        {
            bool exists = false;
            using (IDbConnection conn = Connector.GetConnection())
            {
                exists =
                    conn.Query<int>(
                        string.Format("SELECT ID FROM {0} where ID = @id", this.TableName),
                        new { id = entityId }).Count() == 1;
            }

            return exists;
        }

        /// <summary>
        /// Load a Character by id
        /// </summary>
        /// <param name="entityId">
        /// Id of the Character
        /// </param>
        /// <returns>
        /// DBCharacter object
        /// </returns>
        public T Get(int entityId)
        {
            T entity = default(T);
            using (IDbConnection conn = Connector.GetConnection())
            {
                DynamicParameters whereParameters = new DynamicParameters();
                whereParameters.Add("Id", entityId);
                entity =
                    conn.Query<T>(SqlMapperUtil.CreateGetSQL(this.TableName, whereParameters), whereParameters)
                        .SingleOrDefault();
            }

            return entity;
        }

        /// <summary>
        /// Load all Character data
        /// </summary>
        /// <param name="parameters">
        /// </param>
        /// <param name="transaction">
        /// </param>
        /// <returns>
        /// Collection of DBCharacter
        /// </returns>
        public IEnumerable<T> GetAll(DynamicParameters parameters = null, IDbTransaction transaction = null)
        {
            IEnumerable<T> entities = null;
            using (IDbConnection conn = Connector.GetConnection())
            {
                entities = conn.Query<T>(
                    SqlMapperUtil.CreateGetSQL(this.TableName, parameters),
                    parameters,
                    transaction);
            }

            return entities;
        }

        /// <summary>
        /// </summary>
        /// <param name="entity">
        /// </param>
        /// <param name="parameters">
        /// </param>
        /// <param name="transaction">
        /// </param>
        /// <returns>
        /// </returns>
        public int Save(T entity, DynamicParameters parameters = null, IDbTransaction transaction = null)
        {
            int rowsAffected = 0;

            using (IDbConnection conn = Connector.GetConnection())
            {
                rowsAffected =
                    conn.Execute(
                        SqlMapperUtil.CreateUpdateSQL(
                            this.TableName,
                            (parameters != null) ? parameters : this.getAllParameters()),
                        transaction);
            }

            return rowsAffected;
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private DynamicParameters getAllParameters()
        {
            DynamicParameters parameters = new DynamicParameters(this);
            foreach (string propertyName in CachedProperties.Keys)
            {
                parameters.Add(propertyName, CachedProperties[propertyName].GetValue(this, null));
            }

            return parameters;
        }

        #endregion
    }
}