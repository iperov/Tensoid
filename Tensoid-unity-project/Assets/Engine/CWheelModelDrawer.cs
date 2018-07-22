using System;
using UnityEngine;

public class CWheelModelDrawer
{
	Font m_font;
	GUIStyle m_font_black_st;

	Rect m_left_rect;
	Rect m_right_rect;
	Vector3 m_left_rect_center;
	Vector3 m_right_rect_center;

	float m_square_radius;

	float m_spoke_line_width;

	float m_circle_button_radius;

	Vector3[,] m_circle_buttons;
	Vector3[,] m_spoke_lines;
	Color[] m_spoke_lines_color;
	Vector3[] m_kgf_pos;
	string[] m_kgf_string;
	Rect[] m_kgf_pos_rect;
	GUIStyle[] m_font_kgf_st;

	WheelModel m_wheel_model;


	public CWheelModelDrawer ( WheelModel wheel_model, Rect left_rect, Rect right_rect, string resource_font )
	{
		m_wheel_model = wheel_model;
		
		m_left_rect = left_rect;
		m_right_rect = right_rect;
		m_left_rect_center = new Vector3 ( m_left_rect.center.x, m_left_rect.center.y );
		m_right_rect_center = new Vector3 ( m_right_rect.center.x, m_right_rect.center.y );

		m_square_radius = m_left_rect.width / 2.0f;

		m_spoke_line_width = m_square_radius * 0.01f ;

		m_font = Resources.Load<Font> ( resource_font );

		m_font_black_st = new GUIStyle ();
		m_font_black_st.font = m_font;
		m_font_black_st.fontSize = (int) ( m_square_radius*0.075f );
		m_font_black_st.normal.textColor = Color.black;	
		m_font_black_st.alignment = TextAnchor.MiddleCenter;


		m_circle_button_radius = m_font_black_st.lineHeight / 2.0f;

		m_circle_buttons = new Vector3[m_wheel_model.get_nspokes(),2];

		updateDrawData();
	}

	void updateDrawData()
	{
		int n_spokes = m_wheel_model.get_nspokes();

		m_spoke_lines = new Vector3[n_spokes,2];
		m_spoke_lines_color = new Color[n_spokes];
		m_kgf_pos = new Vector3[n_spokes];
		m_kgf_pos_rect = new Rect[n_spokes];
		m_font_kgf_st = new GUIStyle[n_spokes];
		m_kgf_string = new string[n_spokes];

		double angle = Math.Atan2 (1.0,1.0)*8 / n_spokes;


		double rim_radius = m_wheel_model.get_rim_d_mm() / 2.0 / 1000.0;

		for (int i=0; i < n_spokes; ++i)
		{
			
			Vector3 hub_point = (Vector3)m_wheel_model.get_hub_point(i);
			Vector3 nipple_point = (Vector3)m_wheel_model.get_nipple_point(i);


			Vector3 center;
			if ( (i%2) == 0 )
			{
				center = m_left_rect_center;
			} else
			{
				center = m_right_rect_center;
			}

			Vector3 edgep = new Vector3 ( (float) ( center.x + Math.Sin( i*angle+angle*0.5)*m_square_radius ), (float) ( center.y + Math.Cos( i*angle+angle*0.5)*m_square_radius)  );
			Vector3 center_edge_vec = (edgep - center).normalized;



			Vector3 circle_p2 = edgep 	 - center_edge_vec*(m_circle_button_radius*2.2f);
			Vector3 circle_p1 = circle_p2 - center_edge_vec*(m_circle_button_radius*2.2f);

			m_circle_buttons[i,0] = circle_p1;
			m_circle_buttons[i,1] = circle_p2;

			m_kgf_pos[i] = circle_p1 - center_edge_vec*(m_font_black_st.lineHeight*1.3f);
			m_kgf_pos_rect[i] = new Rect ( m_kgf_pos[i].x-50, (Screen.height - m_kgf_pos[i].y) - 50 , 100, 100 );

			m_kgf_string[i] = string.Format ("{0}", (int)m_wheel_model.get_spoke_kgf(i));


			Vector3 p2 = m_kgf_pos[i] - center_edge_vec*(m_font_black_st.lineHeight);
			Vector3 p1 = center + ( hub_point / (float)rim_radius )* ( (p2-center).magnitude );

			Vector3 hsv = m_wheel_model.get_hsv_stress(i);

			p1.z = 0;
			p2.z = 0;

			Color c = Color.HSVToRGB (hsv.x, hsv.y, hsv.z);

			m_spoke_lines[i,0] = p1;
			m_spoke_lines[i,1] = p2;
			m_spoke_lines_color[i] = c;

			m_font_kgf_st[i] = new GUIStyle ();
			m_font_kgf_st[i].font = m_font;
			m_font_kgf_st[i].fontSize = (int) ( m_square_radius*0.075f );
			m_font_kgf_st[i].normal.textColor = Color.yellow;	
			m_font_kgf_st[i].alignment = TextAnchor.MiddleCenter;



		}

	}

	private Color m_orange_color = new Color(1,0.5f,0);
	public void onPostRender()
	{
		int n_spokes = m_wheel_model.get_nspokes();

		for (int i=0; i < n_spokes; ++i)
		{			
			
			UnityDrawHelper.drawCircle2D ( m_spoke_lines[i,0], m_spoke_line_width*2f, 10, m_spoke_line_width, Color.black );

			UnityDrawHelper.drawLine2D ( m_spoke_lines[i,1], m_spoke_lines[ (i+2)%n_spokes,1], m_spoke_line_width, Color.black, Color.black );

			UnityDrawHelper.drawLine2D ( m_spoke_lines[i,0], m_spoke_lines[i,1], m_spoke_line_width, m_spoke_lines_color[i], m_spoke_lines_color[i] );

			UnityDrawHelper.drawCircle2D ( m_circle_buttons[i,0], m_circle_button_radius, 10, 1, Color.black );
			UnityDrawHelper.drawCircle2D ( m_circle_buttons[i,1], m_circle_button_radius, 10, 1, Color.black );
			UnityDrawHelper.drawMinus2D  ( m_circle_buttons[i,0], m_circle_button_radius*0.7f, 1.5f, Color.cyan );
			UnityDrawHelper.drawPlus2D  ( m_circle_buttons[i,1], m_circle_button_radius*0.7f, 1.5f, m_orange_color );
		}

	}

	public void onGUI()
	{
		int n_spokes = m_wheel_model.get_nspokes();

		for (int i=0; i < n_spokes; ++i)
			GUI.Label ( m_kgf_pos_rect[i], m_kgf_string[i], m_font_kgf_st[i] );

	}

	public int getButtonClicked (Vector2 pos)
	{
		Vector3 _pos = new Vector3( pos.x, pos.y );
		for (int i=0; i< m_wheel_model.get_nspokes(); ++i)
		{
			if ( (_pos - m_circle_buttons[i,0]).magnitude < m_circle_button_radius )
				return -(i+1);

			if ( (_pos - m_circle_buttons[i,1]).magnitude < m_circle_button_radius )
				return (i+1);
		}
		return 0;
	}



}


