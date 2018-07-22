using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;


public class main : MonoBehaviour {
	private Material m_line_mat;

	private Font m_font_consolas;

	private GUIStyle m_font_consolas_black_st;

	private GUIStyle m_font_tension_graph_style;

	WheelModel m_wheel_model;

	public GameObject m_assign_left_graph_rect;
	public GameObject m_assign_right_graph_rect;

	private Matrix4x4 m_screen2d_matrix;

	public GameObject m_modelview_canvas; //editor assign
	public GameObject m_modeloptions_canvas; //editor assign

	private GameObject[] m_spoke_sliders;

	private int m_config_n_spokes = 32;
	private int m_config_n_cross = 3;
	private double m_config_rim_d = 519;
	private double m_config_hub_d = 50;
	private double m_config_axle_length = 100; 
	private double m_config_l_dish = 15;
	private double m_config_r_dish = 15;

	public Slider m_assign_spoke_count_slider;
	public Text m_assign_spoke_count_slider_text; //editor assign
	public Slider m_assign_cross_count_slider;
	public Text m_assign_cross_count_slider_text; //editor assign
	public Slider m_assign_rimd_slider;
	public Text m_assign_rimd_slider_text; //editor assign
	public Slider m_assign_hubd_slider;
	public Text m_assign_hubd_slider_text; //editor assign
	public Slider m_assign_axlel_slider;
	public Text m_assign_axlel_slider_text; //editor assign
	public Slider m_assign_ldish_slider;
	public Text m_assign_ldish_slider_text; //editor assign
	public Slider m_assign_rdish_slider;
	public Text m_assign_rdish_slider_text; //editor assign
	public Text m_assign_bent_text;
	public Text m_assign_egg_text;
	public Button m_assign_random_unbalance_button;
	public Button m_assign_reset_button;

	CWheelModelDrawer m_wheel_model_drawer;
	Vector3 m_wheel_model_drawer_center;
	float m_wheel_model_drawer_radius;

	bool m_wheel_model_reinitialize_disable;


struct CTensionDataGrid
{
	public Vector3[,] m_lines;

	public CTensionDataGrid(int n_sections, float width, float height)
	{
		m_lines = new Vector3[n_sections,2];


	}
};

	void CreateLineMaterial ()
	{
		var shader = Shader.Find ("Hidden/Internal-Colored");
		m_line_mat = new Material (shader);
		m_line_mat.hideFlags = HideFlags.HideAndDontSave;
		// Turn on alpha blending
		m_line_mat.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		m_line_mat.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		// Turn backface culling off
		m_line_mat.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		// Turn off depth writes
		m_line_mat.SetInt ("_ZWrite", 0);
	}

	void Awake()
	{
		//Application.ExternalCall("OnLoaded");
	}

	void updateWheelModelSliders()
	{
		m_wheel_model_reinitialize_disable = true;
		onSpokeCountSlider_valueChanged (m_assign_spoke_count_slider.value);
		onCrossCountSlider_valueChanged (m_assign_cross_count_slider.value);
		onRimdSlider_valueChanged (m_assign_rimd_slider.value);
		onHubdSlider_valueChanged (m_assign_hubd_slider.value);
		onAxlelSlider_valueChanged (m_assign_axlel_slider.value);
		onLdishSlider_valueChanged (m_assign_ldish_slider.value);
		onRdishSlider_valueChanged (m_assign_rdish_slider.value);
		m_wheel_model_reinitialize_disable = false;
	}

	void Start () 
	{
		m_assign_spoke_count_slider.onValueChanged.AddListener ( delegate(float f){ onSpokeCountSlider_valueChanged(f); } );
		m_assign_cross_count_slider.onValueChanged.AddListener ( delegate(float f){ onCrossCountSlider_valueChanged(f); } );
		m_assign_rimd_slider.onValueChanged.AddListener ( delegate(float f) { onRimdSlider_valueChanged(f); } );
		m_assign_hubd_slider.onValueChanged.AddListener ( delegate(float f) { onHubdSlider_valueChanged(f); } );
		m_assign_axlel_slider.onValueChanged.AddListener ( delegate(float f){ onAxlelSlider_valueChanged(f); } );
		m_assign_ldish_slider.onValueChanged.AddListener ( delegate(float f){ onLdishSlider_valueChanged(f); } );
		m_assign_rdish_slider.onValueChanged.AddListener ( delegate(float f){ onRdishSlider_valueChanged(f); } );
		m_assign_random_unbalance_button.onClick.AddListener ( delegate(){ onRandomUnbalanceButtonClick(); } );
		m_assign_reset_button.onClick.AddListener ( delegate(){ onResetButtonClick(); } );

		updateWheelModelSliders();

		m_screen2d_matrix = Matrix4x4.identity;
		m_screen2d_matrix.SetTRS ( Vector3.zero, Quaternion.identity, new Vector3( 1.0f/ Screen.width, 1.0f/ Screen.height, 1.0f) );

		CreateLineMaterial ();

		m_font_consolas = Resources.Load<Font> ( "Fonts/Consolas" );

		m_font_consolas_black_st = new GUIStyle ();
		m_font_consolas_black_st.font = m_font_consolas;
		m_font_consolas_black_st.fontSize = 10;
		m_font_consolas_black_st.normal.textColor = Color.white;	


		m_font_tension_graph_style = new GUIStyle ();
		m_font_tension_graph_style.font = m_font_consolas;
		m_font_tension_graph_style.fontSize = (int) ( ( (float)Screen.height / 768.0 ) * 14 );
		m_font_tension_graph_style.normal.textColor = Color.black;	
		m_font_tension_graph_style.alignment = TextAnchor.MiddleCenter;

		reinitializeModel();
		onScreenResized();
	}

	void reinitializeModel()
	{
		if ( m_wheel_model_reinitialize_disable )
			return;

		m_wheel_model = new WheelModel();
		m_wheel_model.initialize( m_config_n_spokes, m_config_n_cross, m_config_rim_d, m_config_hub_d, m_config_axle_length, m_config_l_dish, m_config_r_dish );
		if ( !m_wheel_model.compute() )
		{
			m_assign_spoke_count_slider.value = 3;
			m_assign_cross_count_slider.value = 3;
			m_assign_rimd_slider.value = 519;
			m_assign_hubd_slider.value = 50;
			m_assign_axlel_slider.value = 100;
			m_assign_ldish_slider.value = 15;
			m_assign_rdish_slider.value = 15;
			updateWheelModelSliders();
			m_wheel_model.reset_balance();
			m_wheel_model.compute();
		}

		updateModelDrawer();
	}

	void updateModelDrawer()
	{
		int smallest_side = Screen.width;
		if ( Screen.height < smallest_side )
			smallest_side = Screen.height;

		m_assign_bent_text.text = string.Format ("{0:0.00} mm", m_wheel_model.get_bent_disbalance_mm() );
		m_assign_egg_text.text = string.Format ("{0:0.00} mm", m_wheel_model.get_egg_disbalance_mm() );

		m_wheel_model_drawer_center = new Vector3 ( Screen.width / 2.0f, Screen.height - Screen.width*0.2f );
		m_wheel_model_drawer_radius = Screen.width*0.13f;

		int screen_w = Screen.width;
		int screen_h = Screen.height;

		screen_w = (int) ( screen_h*1.33333333f );

		Rect left_rect = new Rect ( Screen.width/2 - screen_w/2, Screen.height-screen_w/2, screen_w/2, screen_w/2 );
		Rect right_rect = new Rect ( Screen.width/2, Screen.height-screen_w/2, screen_w/2, screen_w/2 );
		m_wheel_model_drawer = new CWheelModelDrawer ( m_wheel_model, left_rect, right_rect, "Fonts/Consolas" );

		//GC.Collect();
	}

	void onScreenResized()
	{
		m_last_screen_width = Screen.width;
		m_last_screen_height = Screen.height;

		m_screen2d_matrix.SetTRS ( Vector3.zero, Quaternion.identity, new Vector3( 1.0f/ Screen.width, 1.0f/ Screen.height, 1.0f) );

		updateModelDrawer();
	}

	int m_last_screen_width;
	int m_last_screen_height;

    bool m_is_lmb_hold;
    float m_lmb_hold_start;
    int m_frame_count;

	void Update () 
	{
        ++m_frame_count;

		//m_ui_rescaler.onUpdate();
		if ( m_last_screen_width != Screen.width ||
			m_last_screen_height != Screen.height )
		{
			
			onScreenResized();
		}

        if ( Input.GetMouseButtonDown( 0 ) )
        {
            m_is_lmb_hold = true;
            m_lmb_hold_start = Time.realtimeSinceStartup;
        }

         if ( Input.GetMouseButtonUp( 0 ) )
            m_is_lmb_hold = false;

         

		if ( Input.GetMouseButtonDown( 0 ) |
             ( m_is_lmb_hold & (Time.realtimeSinceStartup-m_lmb_hold_start) > 0.2f & (m_frame_count % 4 == 0) )
            
            ) 
		{ 	
			Vector2 pos = Input.mousePosition; 

			int n_spoke = 0;
			int sign = 0;
			int n_button = m_wheel_model_drawer.getButtonClicked(pos);
			if ( n_button != 0 )
			{
				sign = n_button / Math.Abs(n_button);
				n_spoke = Math.Abs(n_button-sign);
			}

			if ( sign != 0 )
			{
				m_wheel_model.rotate_nipple (n_spoke, (double)sign * 1.0/16.0 );
				m_wheel_model.compute();
				updateModelDrawer();

                m_wheel_model.dump_log();
			}
		}


		/*if (Event.current.type == EventType.MouseDown) 
		{ 
			Vector2 pos = Event.current.mousePosition ; 
			Debug.Log (pos.x);
		}*/
	}

	bool draw_once;
	void OnPostRender() 
	{
		draw_once = true;

	}

	void OnGUI()
	{
		m_wheel_model_drawer.onGUI();

		if ( draw_once )
		{
			draw_once=false;
			GL.PushMatrix();
		GL.LoadOrtho();

		GL.MultMatrix ( m_screen2d_matrix );
		m_line_mat.SetPass(0);

		m_wheel_model_drawer.onPostRender();

		GL.PopMatrix();
		}
	}




	public void onSpokeCountSlider_valueChanged(float f)
	{
		switch ( (int)f )
		{
		case 0: m_config_n_spokes = 20; if (m_config_n_cross > 2) m_config_n_cross = 2;break;
        //case 0: m_config_n_spokes = 10; if (m_config_n_cross > 0) m_config_n_cross = 0;break;
		case 1: m_config_n_spokes = 24; if (m_config_n_cross > 3) m_config_n_cross = 3;break;
		case 2: m_config_n_spokes = 28; if (m_config_n_cross > 3) m_config_n_cross = 3;break;
		case 3: m_config_n_spokes = 32; if (m_config_n_cross > 3) m_config_n_cross = 3;break;
		case 4: m_config_n_spokes = 36; if (m_config_n_cross > 4) m_config_n_cross = 4;break;
		}
		m_assign_spoke_count_slider_text.text = "Spoke count: "+m_config_n_spokes;
		reinitializeModel();
	}

	public void onCrossCountSlider_valueChanged(float f)
	{
		m_config_n_cross = (int)f;
		switch ( m_config_n_spokes )
		{
		case 20: if (m_config_n_cross > 2) m_config_n_cross = 2;break;
		case 24: if (m_config_n_cross > 3) m_config_n_cross = 3;break;
		case 28: if (m_config_n_cross > 3) m_config_n_cross = 3;break;
		case 32: if (m_config_n_cross > 3) m_config_n_cross = 3;break;
		case 36: if (m_config_n_cross > 4) m_config_n_cross = 4;break;
		}

		m_assign_cross_count_slider_text.text = "Cross count: "+m_config_n_cross;
		reinitializeModel();
	}

	public void onRimdSlider_valueChanged(float f)
	{
		m_config_rim_d = (int)f;
		if ( m_config_rim_d < m_config_hub_d + 100 )
			m_config_hub_d = m_config_hub_d + 100;

		m_assign_rimd_slider_text.text = "Rim diameter(mm): "+(int)m_config_rim_d;
		reinitializeModel();
	}

	public void onHubdSlider_valueChanged(float f)
	{
		m_config_hub_d = (int)f;
		if ( m_config_hub_d > m_config_rim_d - 100 )
			m_config_hub_d = m_config_rim_d - 100;

		m_assign_hubd_slider_text.text = "Hub diameter(mm): "+(int)m_config_hub_d;
		reinitializeModel();
	}

	public void onAxlelSlider_valueChanged(float f)
	{
		m_config_axle_length = (int)f;
		if ( m_config_axle_length < m_config_l_dish+m_config_r_dish*1.2f )
			m_config_axle_length = m_config_l_dish+m_config_r_dish*1.2f;

		m_assign_axlel_slider_text.text = "Axle length(mm): "+(int)m_config_axle_length;
		reinitializeModel();
	}

	public void onLdishSlider_valueChanged(float f)
	{
		m_config_l_dish = (int)f;
		if ( m_config_l_dish > m_config_axle_length*0.40 )
			m_config_l_dish = m_config_axle_length*0.40;

		m_assign_ldish_slider_text.text = "L dish(mm): "+(int)m_config_l_dish;
		reinitializeModel();
	}

	public void onRdishSlider_valueChanged(float f)
	{
		m_config_r_dish = (int)f;
		if ( m_config_r_dish > m_config_axle_length*0.40 )
			m_config_r_dish = m_config_axle_length*0.40;

		m_assign_rdish_slider_text.text = "R dish(mm): "+(int)m_config_r_dish;
		reinitializeModel();
	}

	private void onRandomUnbalanceButtonClick()
	{
		m_wheel_model.random_unbalance();
		m_wheel_model.compute();
		updateModelDrawer();
	}

	void onResetButtonClick()
	{
		m_wheel_model.reset_balance();
		m_wheel_model.compute();
		updateModelDrawer();
	}

};