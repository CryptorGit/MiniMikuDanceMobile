package com.example.viewer.viewsettings

import android.os.Bundle
import android.view.View
import android.widget.SeekBar
import androidx.fragment.app.Fragment
import com.example.viewer.R

class ViewSettingsFragment : Fragment(R.layout.panel_view) {
    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val listener = object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {}
            override fun onStartTrackingTouch(seekBar: SeekBar?) {}
            override fun onStopTrackingTouch(seekBar: SeekBar?) {}
        }
        view.findViewById<SeekBar>(R.id.seek_morph).setOnSeekBarChangeListener(listener)
        view.findViewById<SeekBar>(R.id.seek_light).setOnSeekBarChangeListener(listener)
    }
}
