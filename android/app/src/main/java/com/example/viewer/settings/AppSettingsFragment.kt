package com.example.viewer.settings

import android.content.Context
import android.os.Bundle
import android.view.View
import android.widget.Switch
import androidx.fragment.app.Fragment
import com.example.viewer.R

class AppSettingsFragment : Fragment(R.layout.panel_settings) {
    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val prefs = requireContext().getSharedPreferences("app_settings", Context.MODE_PRIVATE)
        val switchShadows = view.findViewById<Switch>(R.id.switch_enable_shadows)
        switchShadows.isChecked = prefs.getBoolean("enable_shadows", false)
        switchShadows.setOnCheckedChangeListener { _, isChecked ->
            prefs.edit().putBoolean("enable_shadows", isChecked).apply()
        }
    }
}
