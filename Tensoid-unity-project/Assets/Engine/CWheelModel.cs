using System;
using UnityEngine;

public class WheelModel
{
	struct SpokeData
	{
		public double default_len;
		public double tightened_len;
		
		public Vector3D hub_local_point;
		public Vector3D rim_local_point;
		
		public double F;
		public double F_prev;

		public Vector3 hsv_stress;
	};

	SpokeData[] m_spokes_data;
    SpokeCalcData[] m_cd;

	int m_nspokes;
	int m_ncross;
	double m_rim_d;
	double m_hub_d;
	double m_axle_length;
	double m_l_dish;
	double m_r_dish;
	double m_hub_fl_thickness;
	int m_spoke_tpi;
	double m_spoke_diameter;
	double m_spoke_young_modulus;
	double m_spoke_space;

	Vector3D m_rim_pos;
	Matrix3x3 m_rim_rot;

	System.Random m_random = new System.Random();

	public double m_highest_F;
	public double m_lowest_F;
	public double m_highest_l_F;
	public double m_lowest_l_F;
	public double m_highest_r_F;
	public double m_lowest_r_F;
	public int m_last_compute_count;

	public WheelModel ()
	{

	}

	public void initialize( int _nspokes, int _ncross, double _rim_d, double _hub_d, double _axle_length, 
	                       	double _l_dish, double _r_dish )
	{
        m_rim_pos = Vector3D.zero;
        m_rim_rot = Matrix3x3.identity;

		m_nspokes = _nspokes;
		m_ncross = _ncross;
		m_rim_d = _rim_d;
		m_hub_d = _hub_d;
		m_axle_length = _axle_length;
		m_l_dish = _l_dish;
		m_r_dish = _r_dish;
		m_hub_fl_thickness = 0;
		m_spoke_diameter = 0.002;
		m_spoke_young_modulus = 200e9;
		m_spoke_tpi = 56;
		m_spoke_space = (m_spoke_diameter / 2.0) * (m_spoke_diameter / 2.0) * Math.PI;

		m_spokes_data = new SpokeData[m_nspokes];
        m_cd = new SpokeCalcData[m_nspokes];

		double angle = Math.Atan2 (1.0,1.0)*8 / m_nspokes;

		for (int n=0; n < m_nspokes; ++n)
		{
			int rim_a = n;
			
			int hub_a;
			if ( (n % 4) >= 2)	hub_a = (n - 2*m_ncross) % m_nspokes; else 
				hub_a = (n + 2*m_ncross) % m_nspokes;
			
			double xr = m_rim_d/2*Math.Sin(rim_a*angle+angle*0.5);// 
			double yr = m_rim_d/2*Math.Cos(rim_a*angle+angle*0.5);// 
			
			double xh = m_hub_d/2*Math.Sin(hub_a*angle+angle*0.5);// 
			double yh = m_hub_d/2*Math.Cos(hub_a*angle+angle*0.5);// 
			
			double zr = 0;
			
			double zh;
			if (n % 2 == 0) //l_spoke	
				zh = -m_axle_length/2  + m_l_dish;	else
				zh = m_axle_length/2 - m_r_dish;	
			
			
			if (n % 4 <= 1)
			{
				if (n % 2 == 0) zh += m_hub_fl_thickness/2.0; else     
					zh -= m_hub_fl_thickness/2.0;			
			} else														
			{															
				if (n % 2 == 0) zh -= m_hub_fl_thickness/2.0; else		
					zh += m_hub_fl_thickness/2.0;			
			}
			
			m_spokes_data[n].rim_local_point = new Vector3D (xr /1000.0, yr/1000.0, zr/1000.0);
			m_spokes_data[n].hub_local_point = new Vector3D (xh /1000.0, yh/1000.0, zh/1000.0);
			
			double spoke_len = m_spokes_data[n].rim_local_point.distance (m_spokes_data[n].hub_local_point);

			m_spokes_data[n].default_len = spoke_len;
			m_spokes_data[n].tightened_len = spoke_len;	
		}

		reset_balance();

		m_last_compute_count = 0;
	}

	private struct SpokeCalcData
	{
		public Vector3D rim_move_vec;
		public double F;
		public double TightF;
		public Vector3D torque_axis;
		public double torque_angle;

		public void zero()
		{
			F = 0;
		}
	};

   

    double step ()
    {    
        Vector3D RimC = get_rim_center();

        double cd_TightF_max = 0.0;
        double max_F_diff = 0;

        double highest_F = 0;
        double lowest_F = double.MaxValue;

        for ( int i = 0; i < m_nspokes; ++i )
        {
            Vector3D Nip = get_nipple_point(i);

            Vector3D Hub = get_hub_point(i);
            Vector3D NipHub = Hub - Nip;

            double NipHub_len = NipHub.length();
            double tight_len = m_spokes_data[i].tightened_len;
            double amp = NipHub_len - tight_len;

            if ( amp > 0 )
            { //spoke work only on stretch
                double k = m_spoke_young_modulus * m_spoke_space / tight_len;
                double F = k * amp;

                highest_F = Math.Max( highest_F, F );
                lowest_F = Math.Min( lowest_F, F );

                m_cd[i].F = F;

                m_spokes_data[i].F_prev = m_spokes_data[i].F;
                m_spokes_data[i].F = m_cd[i].F;

                max_F_diff = Math.Max( max_F_diff, Math.Abs( m_spokes_data[i].F - m_spokes_data[i].F_prev ) );

                m_cd[i].TightF = k;

                cd_TightF_max = Math.Max( m_cd[i].TightF, cd_TightF_max );

                Vector3D NipTight = NipHub.normalized() * amp;

                Vector3D NipTightPoint = Nip + NipTight;
                Vector3D RimCNip = Nip - RimC;
                Vector3D RimCNipTightPoint = NipTightPoint - RimC;

                m_cd[i].rim_move_vec = NipTight;
                m_cd[i].torque_axis = RimCNip.cross( RimCNipTightPoint ).normalized();
                m_cd[i].torque_angle = Math.Acos( RimCNipTightPoint.dot( RimCNip ) / (RimCNipTightPoint.length() * RimCNip.length()) );

                continue;

            } else
            {
                m_cd[i].zero();
            }

        }
        m_highest_F = highest_F;
        m_lowest_F = lowest_F;

        Vector3D move_vector = Vector3D.zero;
        Matrix3x3 rotation_matrix = Matrix3x3.identity;        

        if ( cd_TightF_max > 0.0 )
            for ( int i = 0; i < m_nspokes; ++i )
            {
                if ( m_cd[i].F == 0.0 )
                    continue;

                double TightF = 0.1 * ( m_cd[i].TightF / cd_TightF_max );

                move_vector += m_cd[i].rim_move_vec * TightF;
                rotation_matrix = rotation_matrix * Matrix3x3.getRotationMatrix( m_cd[i].torque_axis, m_cd[i].torque_angle * TightF );
            }
        m_rim_pos += move_vector;
        m_rim_rot = m_rim_rot * rotation_matrix;

        return max_F_diff;
    }

	
	public bool compute()
	{
        float time = Time.realtimeSinceStartup;

        int n_cycles = 0;
        for ( ;;)
        {
            ++n_cycles;
            double f_diff = step();
            if ( f_diff < 0.001 )
                break;

            if ( Time.realtimeSinceStartup - time >= 2.0 )
                return false;
        }

		double highest_l_kgf = 0;
		double lowest_l_kgf = double.MaxValue;
		double highest_r_kgf = 0;
		double lowest_r_kgf = double.MaxValue;
		double mid_l_kgf = 0;
		double mid_r_kgf = 0;

		for (int i=0; i< m_nspokes; ++i)
		{
			double kgf = get_spoke_kgf(i);
			if ( (i % 2) == 0 )
			{
                highest_l_kgf = Math.Max(highest_l_kgf, kgf );
                lowest_l_kgf  = Math.Min(lowest_l_kgf, kgf );
				mid_l_kgf += kgf;		
			} else
			{
                highest_r_kgf = Math.Max(highest_r_kgf, kgf );
                lowest_r_kgf  = Math.Min(lowest_r_kgf, kgf );
				mid_r_kgf += kgf;
			}
		}

		mid_l_kgf /= m_nspokes / 2;
		mid_r_kgf /= m_nspokes / 2;

		double highest_kgf = get_highest_kgf();
		double lowest_kgf = get_lowest_kgf();
		double mid_kgf = get_mid_kgf();

		double c_highest_l_kgf = highest_l_kgf;
		double c_lowest_l_kgf = lowest_l_kgf;
		double c_highest_r_kgf = highest_r_kgf;
		double c_lowest_r_kgf = lowest_r_kgf;
		double c_highest_kgf = highest_kgf;
		double c_lowest_kgf = lowest_kgf;

		if (c_highest_l_kgf - mid_l_kgf < mid_l_kgf - c_lowest_l_kgf )
			c_lowest_l_kgf = mid_l_kgf - (c_highest_l_kgf - mid_l_kgf);
		else
			c_highest_l_kgf = mid_l_kgf + (mid_l_kgf - c_lowest_l_kgf);

		if ( c_highest_l_kgf - c_lowest_l_kgf < 0.1 )
		{
			c_highest_l_kgf += 1.0;
			c_lowest_l_kgf -= 1.0;
		}

		if (c_highest_r_kgf - mid_r_kgf < mid_r_kgf - c_lowest_r_kgf )
			c_lowest_r_kgf = mid_r_kgf - (c_highest_r_kgf - mid_r_kgf);
		else
			c_highest_r_kgf = mid_r_kgf + (mid_r_kgf - c_lowest_r_kgf);

		if ( c_highest_r_kgf - c_lowest_r_kgf < 0.1 )
		{
			c_highest_r_kgf += 1.0;
			c_lowest_r_kgf -= 1.0;
		}

		if (c_highest_kgf - mid_kgf < mid_kgf - c_lowest_kgf )
			c_lowest_kgf = mid_kgf - (c_highest_kgf - mid_kgf);
		else
			c_highest_kgf = mid_kgf + (mid_kgf - c_lowest_kgf);

		if ( c_highest_kgf - c_lowest_kgf < 0.1 )
		{
			c_highest_kgf += 1.0;
			c_lowest_kgf -= 1.0;
		}

		double angle = Math.Atan2 (1.0,1.0)*8 / m_nspokes;
		double degrees = 360 / m_nspokes;

		for (int i=0; i< m_nspokes; ++i)
		{
			double kgf = get_spoke_kgf(i);
			double rel = (kgf - c_lowest_kgf) / ( c_highest_kgf - c_lowest_kgf );
			/*if ( (i%2) == 0 )
				rel = (kgf - c_lowest_l_kgf) / ( c_highest_l_kgf - c_lowest_l_kgf );
			else
				rel = (kgf - c_lowest_r_kgf) / ( c_highest_r_kgf - c_lowest_r_kgf );
			*/
			if ( rel < 0.0 )
				rel = 0.0;
			else
			if ( rel > 1.0 )
				rel = 1.0;

			m_spokes_data[i].hsv_stress = new Vector3 ( (1.0f-(float)rel) * ( 163f/255f), 1f, 1f );	
		}

		return true;
	}

	public void rotate_nipple (int n_spoke, double rev)
	{
		m_spokes_data[n_spoke].tightened_len -= (0.0254 / m_spoke_tpi)*rev;
	}

	public void reset_balance()
	{
		for (int i=0; i<m_nspokes; ++i)
			m_spokes_data[i].tightened_len = m_spokes_data[i].default_len;

       // int ncount = 0;

        for (int i=0; i<m_nspokes; ++i)
		{
			double diff = ( 0.0254d / m_spoke_tpi )*(1.0/8)*8;
			m_spokes_data[i].tightened_len -= diff;
       } 
       // compute();
       
		for (;;)
		{
			if ( get_lowest_kgf() >= 100 )
				break;

			for (int i=0; i<m_nspokes; ++i)
			{
				double diff = ( 0.0254d / m_spoke_tpi )*(1.0/32);
				m_spokes_data[i].tightened_len -= diff;
             }
           // ++ncount;

			compute();
		}
	}

	public void random_unbalance()
	{
		for (int i=0; i< m_nspokes; ++i)
		{
			double rev = ( (double) ( m_random.Next() % 500) - 250 ) / 1000.0;
			m_spokes_data[i].tightened_len -= (0.0254 / m_spoke_tpi)*rev;
		}

	}

	public Vector3D get_hub_center()
	{
		return new Vector3D(0,0,0);
	}

	public Vector3D get_rim_center()
	{
		return m_rim_pos;
	}

	public Vector3D get_nipple_point(int n_spoke)
	{	
		return m_rim_pos + m_rim_rot * m_spokes_data[n_spoke].rim_local_point;
	}

    public Vector3D get_nipple_point_perp_axis ( int n_spoke )
    {
        return ((m_rim_pos + m_rim_rot * (m_spokes_data[n_spoke].rim_local_point + new Vector3D( 0, 0, 1.0 ))) //left
         - (m_rim_pos + m_rim_rot * m_spokes_data[n_spoke].rim_local_point)).normalized();
    }

	public Vector3D get_hub_point(int n_spoke)
	{
		return m_spokes_data[n_spoke].hub_local_point;
	}

	public double get_egg_disbalance_mm()
	{
		Vector3D v = m_rim_pos;
		v.z = 0;
		return v.length() * 1000.0;
	}

    public void dump_log()
    {
        for ( int i = 0; i < m_nspokes; ++i )
        {
            Vector3D x = get_nipple_point(i) - get_rim_center();

            UnityEngine.Debug.Log( string.Format("{0} : {1} : {2}", i, get_spoke_kgf( i ), x.z * 1000.0f ) );
        }
    }

	public double get_bent_disbalance_mm()
	{
		//check Z at nipples
		double min_z = 99999999;
		double max_z = 0;

		for (int i=0; i< m_nspokes; ++i)
		{
			Vector3D x = get_nipple_point(i) - get_rim_center();

			if ( x.z < min_z )
				min_z = x.z;

			if ( x.z > max_z )
				max_z = x.z;
		}

		return Math.Abs(max_z-min_z) * 1000.0;
	}

    /// <summary>
    /// >= 0.0 & < 360.0
    /// </summary>
    /// <param name="angle_deg_f"></param>
    public void getRimPoint (double angle_deg_f)
    {
        //0 spoke - left

        double angle_rad = angle_deg_f * Math.PI / 180.0;

        double angle_rad_per_2_spokes = (Math.PI*2) / (m_nspokes/2);

        double two_spokes_idf = angle_rad / angle_rad_per_2_spokes;

        int two_spokes_id = (int)two_spokes_idf;

        //m_spokes_data[0].hub_local_point
        //m_spokes_data[0].rim_local_point


        //

        Vector3D hub_nipple_world_F = get_hub_point(0) - get_nipple_point(0);
        Vector3D nipple_point_perp_axis = get_nipple_point_perp_axis(0);

        double perp_F = ( nipple_point_perp_axis * nipple_point_perp_axis.dot(hub_nipple_world_F) / hub_nipple_world_F.length2() ).length();


        //if ( (i % 2) == 0 ) //left

        Vector3D rim_perp_world_axis =
          ( ( m_rim_pos + m_rim_rot *(m_spokes_data[0].rim_local_point + new Vector3D(0,0,1.0)) ) //left
           - (m_rim_pos + m_rim_rot * m_spokes_data[0].rim_local_point) ).normalized();

        Vector3D rim_perp_world_axis2 =
          ( ( m_rim_pos + m_rim_rot *(m_spokes_data[1].rim_local_point - new Vector3D(0,0,1.0)) ) //right
           - (m_rim_pos + m_rim_rot * m_spokes_data[1].rim_local_point) ).normalized();
        //project force

        //Math.c

        double radius =  m_rim_d / 2;//- egg bent 0.00mm

        double xr = radius * Math.Sin( angle_rad ); 
        double yr = radius * Math.Cos( angle_rad );
        double zr = 0.0; //add bent

        new Vector3D( xr, yr, zr );


        //m_rim_pos +  * m_rim_rot
    }

	//Dynamic data
	public double get_spoke_force(int n_spoke)	{	return m_spokes_data[n_spoke].F;		}
	public double get_spoke_kgf(int n_spoke)	{	return get_spoke_force(n_spoke) * 0.101972;		}
	public double get_highest_kgf()		{	return m_highest_F * 0.101972;	}
	public double get_lowest_kgf()		{	return m_lowest_F * 0.101972;	}
	public double get_mid_kgf()	
	{
		double kgf = 0;
		for (int i=0; i<m_nspokes; ++i)
			kgf += get_spoke_kgf(i);

		return kgf / m_nspokes;
	}
	public Vector3 get_hsv_stress(int n_spoke)	{	return m_spokes_data[n_spoke].hsv_stress;	}
	//Static data
	public int get_nspokes()					{	return m_nspokes;				}
	public int get_ncross()						{	return m_ncross;				}
	public double get_rim_d_mm()				{	return m_rim_d;					}
	public double get_hub_d_mm()					{	return m_hub_d;					}
	public double get_axle_length()				{	return m_axle_length;			}
	public double get_l_dish()					{	return m_l_dish;				}
	public double get_r_dish()					{	return m_r_dish;				}
	public double get_fl_thickness()			{	return m_hub_fl_thickness;		}
	public int get_spoke_tpi()					{	return m_spoke_tpi;				}
	public double get_spoke_diameter()			{	return m_spoke_diameter;		}
	public double get_spoke_young_modulus()		{	return m_spoke_young_modulus;	}	


}


/*
public CComputeResult ( CWheelModel m_wheel_model )
		{
			m_nspokes = m_wheel_model.get_nspokes();
			m_spokes_data = new CSpokeData[m_nspokes];

			double angle = Math.Atan2 (1.0,1.0)*8 / m_nspokes;
			double degrees = 360 / m_nspokes;

			double highest_kgf = m_wheel_model.get_highest_kgf();
			double lowest_kgf = m_wheel_model.get_lowest_kgf();
			double mid_kgf = m_wheel_model.get_mid_kgf();

			double c_highest_kgf = highest_kgf;
			double c_lowest_kgf = lowest_kgf;

			if (c_highest_kgf - mid_kgf < mid_kgf - c_lowest_kgf )
				c_lowest_kgf = mid_kgf - (highest_kgf - mid_kgf);
			else
				c_highest_kgf = mid_kgf + (mid_kgf - c_lowest_kgf);

			if ( c_highest_kgf - c_lowest_kgf < 0.1 )
			{
				c_highest_kgf += 1.0;
				c_lowest_kgf -= 1.0;
			}



			for (int i=0; i< m_nspokes; ++i)
			{
				double kgf = m_wheel_model.get_spoke_kgf(i);

				double rel = (kgf - c_lowest_kgf) / ( c_highest_kgf - c_lowest_kgf );
				if ( rel < 0.0 )
					rel = 0.0;
				else
				if ( rel > 1.0 )
					rel = 1.0;
		
				double per = kgf / highest_kgf;

				m_spokes_data[i].rel = per;
				m_spokes_data[i].point =  new Vector3 ( (float) (per * Math.Sin( i*angle + angle*0.5)),
														(float)	(per * Math.Cos( i*angle + angle*0.5)) );

				m_spokes_data[i].point_max = new Vector3 ( (float) (1.0 * Math.Sin( i*angle + angle*0.5)),
												(float)	(1.0 * Math.Cos( i*angle + angle*0.5)) );

				m_spokes_data[i].hsv_color = new Vector3 ( (1f-(float)rel) * ( 163f/255f), 1f, 1f );	
			}

		}
		*/