using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    public class CDTipoPago
    {
        public List<CMTipoPago> ListarTipoPagos()
        {
            List<CMTipoPago> lista = new List<CMTipoPago>();
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("LISTAR_TipoPago", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new CMTipoPago
                    {
                        id_tipoPago = Convert.ToInt32(dr["id_tipoPago"]),
                        nombre = dr["nombre"].ToString(),  
                        descripcion = dr["descripcion"].ToString()
                    });
                }
            }
            return lista;
        }

        public bool AgregarTipoPago(CMTipoPago tipoPago)
        {
            bool resultado = false;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("ADICIONAR_TipoPago", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@nombre", tipoPago.nombre); 
                cmd.Parameters.AddWithValue("@descripcion", tipoPago.descripcion);
                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public bool ModificarTipoPago(CMTipoPago tipoPago)
        {
            bool resultado = false;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("MODIFICAR_TipoPago", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_tipoPago", tipoPago.id_tipoPago);
                cmd.Parameters.AddWithValue("@nombre", tipoPago.nombre);  // NUEVO PARÁMETRO
                cmd.Parameters.AddWithValue("@descripcion", tipoPago.descripcion);
                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public bool EliminarTipoPago(int id)
        {
            bool resultado = false;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("ELIMINAR_TipoPago", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_tipoPago", id);
                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public CMTipoPago ObtenerTipoPagoPorId(int id)
        {
            CMTipoPago tipoPago = null;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("OBTENER_TipoPago_PorId", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_tipoPago", id);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    tipoPago = new CMTipoPago
                    {
                        id_tipoPago = Convert.ToInt32(dr["id_tipoPago"]),
                        nombre = dr["nombre"].ToString(),  
                        descripcion = dr["descripcion"].ToString()
                    };
                }
            }
            return tipoPago;
        }
    }
}